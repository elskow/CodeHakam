using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountService.Models;

[Table("casbin_rule", Schema = "users")]
public sealed class CasbinRule
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("ptype")]
    [MaxLength(100)]
    public string? PType { get; set; }

    [Column("v0")]
    [MaxLength(100)]
    public string? V0 { get; set; }

    [Column("v1")]
    [MaxLength(100)]
    public string? V1 { get; set; }

    [Column("v2")]
    [MaxLength(100)]
    public string? V2 { get; set; }

    [Column("v3")]
    [MaxLength(100)]
    public string? V3 { get; set; }

    [Column("v4")]
    [MaxLength(100)]
    public string? V4 { get; set; }

    [Column("v5")]
    [MaxLength(100)]
    public string? V5 { get; set; }
}
