using Diplom.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Diplom.Services;

public static class PurchaseOrderIngredientsSchemaPatch
{
    public static async Task ApplyAsync(DiplomDbContext context, CancellationToken cancellationToken = default)
{
    // 1. Добавляем колонку IngredientID (если её ещё нет)
    await context.Database.ExecuteSqlRawAsync(@"
        IF OBJECT_ID(N'dbo.PurchaseOrderDetails', N'U') IS NULL 
            RETURN;

        IF COL_LENGTH(N'dbo.PurchaseOrderDetails', N'IngredientID') IS NULL
        BEGIN
            ALTER TABLE dbo.PurchaseOrderDetails
            ADD IngredientID INT NULL;
            PRINT 'Column IngredientID added.';
        END
        ELSE
        BEGIN
            PRINT 'Column IngredientID already exists.';
        END;", cancellationToken);

    // 2. Делаем ProductID nullable (если нужно)
    await context.Database.ExecuteSqlRawAsync(@"
        IF EXISTS (
            SELECT 1 FROM sys.columns 
            WHERE object_id = OBJECT_ID(N'dbo.PurchaseOrderDetails')
              AND name = N'ProductID' 
              AND is_nullable = 0
        )
        BEGIN
            ALTER TABLE dbo.PurchaseOrderDetails
            ALTER COLUMN ProductID INT NULL;
            PRINT 'Column ProductID made nullable.';
        END;", cancellationToken);

    // 3. Добавляем Foreign Key (если нет)
    await context.Database.ExecuteSqlRawAsync(@"
        IF NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys 
            WHERE name = N'FK_PurchaseOrderDetails_Ingredient'
        )
        BEGIN
            ALTER TABLE dbo.PurchaseOrderDetails WITH NOCHECK
            ADD CONSTRAINT FK_PurchaseOrderDetails_Ingredient
                FOREIGN KEY (IngredientID) REFERENCES dbo.Ingredients(IngredientID);
            PRINT 'FK FK_PurchaseOrderDetails_Ingredient added.';
        END;", cancellationToken);

    // 4. Создаём индекс (если нет)
    await context.Database.ExecuteSqlRawAsync(@"
        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes 
            WHERE object_id = OBJECT_ID(N'dbo.PurchaseOrderDetails')
              AND name = N'IX_PurchaseOrderDetails_IngredientID'
        )
        BEGIN
            CREATE INDEX IX_PurchaseOrderDetails_IngredientID
                ON dbo.PurchaseOrderDetails(IngredientID);
            PRINT 'Index IX_PurchaseOrderDetails_IngredientID created.';
        END;", cancellationToken);

    // 5. Заполняем данные (самый важный шаг — отдельно!)
    await context.Database.ExecuteSqlRawAsync(@"
        UPDATE pod
        SET IngredientID = i.IngredientID
        FROM dbo.PurchaseOrderDetails AS pod
        INNER JOIN dbo.Products AS p ON p.ProductID = pod.ProductID
        INNER JOIN dbo.Ingredients AS i ON i.Name = p.NameProduct
        WHERE pod.IngredientID IS NULL;

        PRINT 'UPDATE completed. Rows affected: ' + CAST(@@ROWCOUNT AS VARCHAR(10));", cancellationToken);
}
}
