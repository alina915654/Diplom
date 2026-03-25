using System.Globalization;
using ClosedXML.Excel;
using Diplom.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Diplom.Services;

public sealed class BackOfficeExportService
{
    public byte[] CreateReportsExcel(ReportsIndexViewModel model)
    {
        using var workbook = new XLWorkbook();

        var summarySheet = workbook.Worksheets.Add("Сводка");
        summarySheet.Cell(1, 1).Value = "Показатель";
        summarySheet.Cell(1, 2).Value = "Значение";

        var summaryRows = new (string Label, string Value)[]
        {
            ("Заказов сегодня", model.OrdersToday.ToString(CultureInfo.InvariantCulture)),
            ("Выручка сегодня", model.RevenueToday.ToString("N2", CultureInfo.InvariantCulture)),
            ("Заказов за месяц", model.OrdersThisMonth.ToString(CultureInfo.InvariantCulture)),
            ("Выручка за месяц", model.RevenueThisMonth.ToString("N2", CultureInfo.InvariantCulture)),
            ("Средний чек за месяц", model.AverageCheckThisMonth.ToString("N2", CultureInfo.InvariantCulture))
        };

        for (var index = 0; index < summaryRows.Length; index++)
        {
            var row = index + 2;
            summarySheet.Cell(row, 1).Value = summaryRows[index].Label;
            summarySheet.Cell(row, 2).Value = summaryRows[index].Value;
        }

        FormatHeader(summarySheet.Range(1, 1, 1, 2));
        summarySheet.Columns().AdjustToContents();

        var dailySheet = workbook.Worksheets.Add("Продажи по дням");
        dailySheet.Cell(1, 1).Value = "Дата";
        dailySheet.Cell(1, 2).Value = "Заказов";
        dailySheet.Cell(1, 3).Value = "Выручка";
        dailySheet.Cell(1, 4).Value = "% от максимума";

        for (var index = 0; index < model.DailySalesChart.Count; index++)
        {
            var row = index + 2;
            var item = model.DailySalesChart[index];
            dailySheet.Cell(row, 1).Value = item.Date;
            dailySheet.Cell(row, 2).Value = item.OrdersCount;
            dailySheet.Cell(row, 3).Value = item.Revenue;
            dailySheet.Cell(row, 4).Value = item.PercentOfMax / 100d;
        }

        dailySheet.Column(1).Style.DateFormat.Format = "dd.MM.yyyy";
        dailySheet.Column(3).Style.NumberFormat.Format = "#,##0.00";
        dailySheet.Column(4).Style.NumberFormat.Format = "0.00%";
        FormatHeader(dailySheet.Range(1, 1, 1, 4));
        dailySheet.Columns().AdjustToContents();

        var salesTypeSheet = workbook.Worksheets.Add("Структура продаж");
        salesTypeSheet.Cell(1, 1).Value = "Тип";
        salesTypeSheet.Cell(1, 2).Value = "Заказов";
        salesTypeSheet.Cell(1, 3).Value = "Выручка";
        salesTypeSheet.Cell(1, 4).Value = "Доля";

        for (var index = 0; index < model.SalesByType.Count; index++)
        {
            var row = index + 2;
            var item = model.SalesByType[index];
            salesTypeSheet.Cell(row, 1).Value = item.TypeName;
            salesTypeSheet.Cell(row, 2).Value = item.OrdersCount;
            salesTypeSheet.Cell(row, 3).Value = item.Revenue;
            salesTypeSheet.Cell(row, 4).Value = item.PercentOfTotal / 100d;
        }

        salesTypeSheet.Column(3).Style.NumberFormat.Format = "#,##0.00";
        salesTypeSheet.Column(4).Style.NumberFormat.Format = "0.00%";
        FormatHeader(salesTypeSheet.Range(1, 1, 1, 4));
        salesTypeSheet.Columns().AdjustToContents();

        var employeesSheet = workbook.Worksheets.Add("Сотрудники");
        employeesSheet.Cell(1, 1).Value = "Сотрудник";
        employeesSheet.Cell(1, 2).Value = "Роль";
        employeesSheet.Cell(1, 3).Value = "Продаж";
        employeesSheet.Cell(1, 4).Value = "Выручка";

        for (var index = 0; index < model.EmployeePerformance.Count; index++)
        {
            var row = index + 2;
            var item = model.EmployeePerformance[index];
            employeesSheet.Cell(row, 1).Value = item.FullName;
            employeesSheet.Cell(row, 2).Value = item.RoleName;
            employeesSheet.Cell(row, 3).Value = item.SalesCount;
            employeesSheet.Cell(row, 4).Value = item.TotalAmount;
        }

        employeesSheet.Column(4).Style.NumberFormat.Format = "#,##0.00";
        FormatHeader(employeesSheet.Range(1, 1, 1, 4));
        employeesSheet.Columns().AdjustToContents();

        var salesSheet = workbook.Worksheets.Add("Последние продажи");
        salesSheet.Cell(1, 1).Value = "Чек";
        salesSheet.Cell(1, 2).Value = "Дата";
        salesSheet.Cell(1, 3).Value = "Клиент";
        salesSheet.Cell(1, 4).Value = "Кассир";
        salesSheet.Cell(1, 5).Value = "Статус";
        salesSheet.Cell(1, 6).Value = "Сумма";

        for (var index = 0; index < model.RecentSales.Count; index++)
        {
            var row = index + 2;
            var item = model.RecentSales[index];
            salesSheet.Cell(row, 1).Value = item.CheckNumber;
            salesSheet.Cell(row, 2).Value = item.SaleDate;
            salesSheet.Cell(row, 3).Value = item.ClientName;
            salesSheet.Cell(row, 4).Value = item.CashierName;
            salesSheet.Cell(row, 5).Value = item.StatusName;
            salesSheet.Cell(row, 6).Value = item.FinalAmount;
        }

        salesSheet.Column(2).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
        salesSheet.Column(6).Style.NumberFormat.Format = "#,##0.00";
        FormatHeader(salesSheet.Range(1, 1, 1, 6));
        salesSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] CreatePurchaseOrdersExcel(PurchaseOrdersIndexViewModel model)
    {
        using var workbook = new XLWorkbook();

        var ordersSheet = workbook.Worksheets.Add("Закупки");
        ordersSheet.Cell(1, 1).Value = "№";
        ordersSheet.Cell(1, 2).Value = "Поставщик";
        ordersSheet.Cell(1, 3).Value = "Статус";
        ordersSheet.Cell(1, 4).Value = "Дата заказа";
        ordersSheet.Cell(1, 5).Value = "Ожидаемая поставка";
        ordersSheet.Cell(1, 6).Value = "Позиций";
        ordersSheet.Cell(1, 7).Value = "Сумма";

        for (var index = 0; index < model.PurchaseOrders.Count; index++)
        {
            var row = index + 2;
            var order = model.PurchaseOrders[index];
            ordersSheet.Cell(row, 1).Value = order.Number;
            ordersSheet.Cell(row, 2).Value = order.SupplierName;
            ordersSheet.Cell(row, 3).Value = order.Status;
            ordersSheet.Cell(row, 4).Value = order.OrderDate;
            ordersSheet.Cell(row, 5).Value = order.ExpectedArrivalDate;
            ordersSheet.Cell(row, 6).Value = order.ItemsCount;
            ordersSheet.Cell(row, 7).Value = order.TotalAmount;
        }

        ordersSheet.Column(4).Style.DateFormat.Format = "dd.MM.yyyy";
        ordersSheet.Column(5).Style.DateFormat.Format = "dd.MM.yyyy";
        ordersSheet.Column(7).Style.NumberFormat.Format = "#,##0.00";
        FormatHeader(ordersSheet.Range(1, 1, 1, 7));
        ordersSheet.Columns().AdjustToContents();

        var detailsSheet = workbook.Worksheets.Add("Состав закупок");
        detailsSheet.Cell(1, 1).Value = "№ закупки";
        detailsSheet.Cell(1, 2).Value = "Поставщик";
        detailsSheet.Cell(1, 3).Value = "Ингредиент";
        detailsSheet.Cell(1, 4).Value = "Заказано";
        detailsSheet.Cell(1, 5).Value = "Принято";
        detailsSheet.Cell(1, 6).Value = "Цена";
        detailsSheet.Cell(1, 7).Value = "Сумма";

        var detailRow = 2;
        foreach (var order in model.PurchaseOrders)
        {
            foreach (var detail in order.Details)
            {
                detailsSheet.Cell(detailRow, 1).Value = order.Number;
                detailsSheet.Cell(detailRow, 2).Value = order.SupplierName;
                detailsSheet.Cell(detailRow, 3).Value = detail.IngredientName;
                detailsSheet.Cell(detailRow, 4).Value = detail.OrderedQty;
                detailsSheet.Cell(detailRow, 5).Value = detail.ReceivedQty;
                detailsSheet.Cell(detailRow, 6).Value = detail.UnitCost;
                detailsSheet.Cell(detailRow, 7).Value = detail.LineTotal;
                detailRow++;
            }
        }

        detailsSheet.Column(6).Style.NumberFormat.Format = "#,##0.00";
        detailsSheet.Column(7).Style.NumberFormat.Format = "#,##0.00";
        FormatHeader(detailsSheet.Range(1, 1, 1, 7));
        detailsSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] CreateReportsPdf(ReportsIndexViewModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text("Отчет по продажам").FontSize(20).SemiBold().FontColor("#6C006C");
                    column.Item().Text($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => BuildMetricCard(c, "Заказов сегодня", model.OrdersToday.ToString(CultureInfo.InvariantCulture)));
                        row.RelativeItem().Element(c => BuildMetricCard(c, "Выручка сегодня", $"{model.RevenueToday:N2} ₽"));
                        row.RelativeItem().Element(c => BuildMetricCard(c, "Заказов за месяц", model.OrdersThisMonth.ToString(CultureInfo.InvariantCulture)));
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => BuildMetricCard(c, "Выручка за месяц", $"{model.RevenueThisMonth:N2} ₽"));
                        row.RelativeItem().Element(c => BuildMetricCard(c, "Средний чек", $"{model.AverageCheckThisMonth:N2} ₽"));
                        row.RelativeItem().Element(c => BuildMetricCard(c, "Статусов", model.StatusSummary.Count.ToString(CultureInfo.InvariantCulture)));
                    });

                    column.Item().Text("Продажи по дням").FontSize(14).SemiBold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(120);
                        });

                        BuildTableHeader(table, "Дата", "Заказов", "Выручка");

                        foreach (var item in model.DailySalesChart)
                        {
                            table.Cell().Element(CellStyle).Text(item.Date.ToString("dd.MM.yyyy"));
                            table.Cell().Element(CellStyle).Text(item.OrdersCount.ToString(CultureInfo.InvariantCulture));
                            table.Cell().Element(CellStyle).Text($"{item.Revenue:N2} ₽");
                        }
                    });

                    column.Item().Text("Последние продажи").FontSize(14).SemiBold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.ConstantColumn(90);
                        });

                        BuildTableHeader(table, "Чек", "Клиент", "Кассир", "Сумма");

                        foreach (var sale in model.RecentSales.Take(12))
                        {
                            table.Cell().Element(CellStyle).Text(sale.CheckNumber);
                            table.Cell().Element(CellStyle).Text(sale.ClientName);
                            table.Cell().Element(CellStyle).Text(sale.CashierName);
                            table.Cell().Element(CellStyle).Text($"{sale.FinalAmount:N2} ₽");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Страница ");
                    text.CurrentPageNumber();
                    text.Span(" из ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    public byte[] CreatePurchaseOrdersPdf(PurchaseOrdersIndexViewModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text("Реестр закупок").FontSize(20).SemiBold().FontColor("#6C006C");
                    column.Item().Text($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(14);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => BuildMetricCard(c, "Всего закупок", model.TotalOrders.ToString(CultureInfo.InvariantCulture)));
                        row.RelativeItem().Element(c => BuildMetricCard(c, "В работе", model.OrdersInProgress.ToString(CultureInfo.InvariantCulture)));
                        row.RelativeItem().Element(c => BuildMetricCard(c, "Получено", model.OrdersReceived.ToString(CultureInfo.InvariantCulture)));
                    });

                    column.Item().Text("Список закупок").FontSize(14).SemiBold();
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(56);
                            columns.RelativeColumn();
                            columns.ConstantColumn(90);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                        });

                        BuildTableHeader(table, "№", "Поставщик", "Статус", "Позиций", "Сумма");

                        foreach (var order in model.PurchaseOrders)
                        {
                            table.Cell().Element(CellStyle).Text(order.Number.ToString(CultureInfo.InvariantCulture));
                            table.Cell().Element(CellStyle).Text(order.SupplierName);
                            table.Cell().Element(CellStyle).Text(order.Status);
                            table.Cell().Element(CellStyle).Text(order.ItemsCount.ToString(CultureInfo.InvariantCulture));
                            table.Cell().Element(CellStyle).Text($"{order.TotalAmount:N2} ₽");
                        }
                    });

                    if (model.PurchaseOrders.Count > 0)
                    {
                        column.Item().Text("Первые позиции закупок").FontSize(14).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(56);
                                columns.RelativeColumn();
                                columns.ConstantColumn(72);
                                columns.ConstantColumn(90);
                            });

                            BuildTableHeader(table, "№", "Ингредиент", "Кол-во", "Цена");

                            foreach (var detail in model.PurchaseOrders
                                         .SelectMany(order => order.Details.Select(detail => new { order.Number, Detail = detail }))
                                         .Take(18))
                            {
                                table.Cell().Element(CellStyle).Text(detail.Number.ToString(CultureInfo.InvariantCulture));
                                table.Cell().Element(CellStyle).Text(detail.Detail.IngredientName);
                                table.Cell().Element(CellStyle).Text(detail.Detail.OrderedQty.ToString(CultureInfo.InvariantCulture));
                                table.Cell().Element(CellStyle).Text($"{detail.Detail.UnitCost:N2} ₽");
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Страница ");
                    text.CurrentPageNumber();
                    text.Span(" из ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void FormatHeader(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#EFD8F7");
        range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void BuildMetricCard(IContainer container, string title, string value)
    {
        container
            .Padding(12)
            .Background("#F8ECFB")
            .Border(1)
            .BorderColor("#E1B7F0")
            .CornerRadius(12)
            .Column(column =>
            {
                column.Item().Text(title).FontSize(10).FontColor("#7B5D88");
                column.Item().Text(value).FontSize(16).SemiBold().FontColor("#6C006C");
            });
    }

    private static void BuildTableHeader(TableDescriptor table, params string[] headers)
    {
        foreach (var header in headers)
        {
            table.Cell().Element(HeaderCellStyle).Text(header).SemiBold().FontColor("#3B2045");
        }
    }

    private static IContainer HeaderCellStyle(IContainer container) =>
        container
            .Background("#EFD8F7")
            .Border(1)
            .BorderColor("#E1B7F0")
            .PaddingVertical(6)
            .PaddingHorizontal(8);

    private static IContainer CellStyle(IContainer container) =>
        container
            .BorderBottom(1)
            .BorderColor("#EEDFF3")
            .PaddingVertical(5)
            .PaddingHorizontal(8);
}
