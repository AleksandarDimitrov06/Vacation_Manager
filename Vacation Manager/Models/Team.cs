using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Vacation_Manager.Models
{
    public class Team
    {
        [Key]
        public int TeamId { get; set; }

        [Required(ErrorMessage = "Името на екипа е задължително")]
        [StringLength(100, ErrorMessage = "Името на екипа не може да бъде по-дълго от 100 символа")]
        public required string TeamName { get; set; }

        public int? ProjectId { get; set; } = null;

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; } = null;

        public string? TeamLeaderId { get; set; } = null;

        [ForeignKey("TeamLeaderId")]
        public virtual User? TeamLeader { get; set; } = null;

        public virtual ICollection<User> Members { get; set; } = new List<User>();
    }
}
