using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountService.Models;

[Table("role_permissions", Schema = "users")]
public sealed class RolePermission
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("role_id")]
    public long RoleId { get; set; }

    [Required]
    [Column("permission_id")]
    public long PermissionId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("RoleId")]
    public Role Role { get; set; } = null!;

    [ForeignKey("PermissionId")]
    public Permission Permission { get; set; } = null!;
}
