using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Vacation_Manager.Models
{
    public class Team
    {
        [Key]
        public int TeamId { get; set; }

        [Required]
        [StringLength(50)]
        public required string TeamName { get; set; }

        public int ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        public string? TeamLeaderId { get; set; }

        [ForeignKey("TeamLeaderId")]
        public virtual User? TeamLeader { get; set; }
        public virtual ICollection<User>? Members { get; set; }
    }
}
