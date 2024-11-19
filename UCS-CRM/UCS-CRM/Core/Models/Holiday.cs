namespace UCS_CRM.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;

public class Holiday
{
    public int Id { get; set; }
    
    [Required]
    [Display(Name = "Holiday Name")]
    public string Name { get; set; }
    
    [Required]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }
    
    [Required]
    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    [CustomValidation(typeof(Holiday), nameof(ValidateEndDate))]
    public DateTime EndDate { get; set; }
    
    [Display(Name = "Description")]
    public string Description { get; set; }
    
    [Required]
    public bool IsRecurring { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? UpdatedDate { get; set; }
    public DateTime? DeletedDate { get; set; }

    public static ValidationResult ValidateEndDate(DateTime endDate, ValidationContext context)
    {
        var holiday = (Holiday)context.ObjectInstance;
        if (endDate < holiday.StartDate)
        {
            return new ValidationResult("End date must be equal to or later than start date");
        }
        return ValidationResult.Success;
    }
}
