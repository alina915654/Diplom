using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Fio { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public int RoleId { get; set; }

    public string Phone { get; set; } = null!;

    public DateOnly? BirthDate { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public DateOnly? HireDate { get; set; }

    public DateOnly? DismissalDate { get; set; }

    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();

    public virtual ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    public virtual ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();
}
