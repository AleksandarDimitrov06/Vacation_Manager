using System.ComponentModel.DataAnnotations;

namespace Vacation_Manager.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(20)]
        public string RoleName { get; set; }
        public virtual ICollection<User> Users { get; set; }
    }
}
