using System;
using System.Collections.Generic;

namespace Diplom.Models;

public partial class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public byte[]? RoleImage { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
