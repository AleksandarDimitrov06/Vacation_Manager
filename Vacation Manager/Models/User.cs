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
        public string LastName { get; set; }

        [Required]
        public int? TeamId { get; set; }

        [ForeignKey("TeamId")]

        [Required]
        public virtual Team Team { get; set; }

        public virtual ICollection<VacationRequest> RequestedVacations { get; set; }
        public virtual ICollection<VacationRequest> ApprovedVacations { get; set; }

        public int? LedTeamId { get; set; } = null;
        public virtual Team? LedTeam { get; set; } = null;
    }
}
