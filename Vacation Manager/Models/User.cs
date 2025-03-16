using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Vacation_Manager.Models
{
    public class User : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public required string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public required string LastName { get; set; }


        public int? TeamId { get; set; } = null;

        [ForeignKey("TeamId")]

       
        public virtual Team? Team { get; set; } = null;


        public int? LedTeamId { get; set; } = null;
        [ForeignKey("LedTeamId")]
        public virtual Team? LedTeam { get; set; } = null;

        public virtual ICollection<VacationRequest>? RequestedVacations { get; set; }
        public virtual ICollection<VacationRequest>? ApprovedVacations { get; set; }
    }
}
