using BMSBT.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace BMSBT.DropDownHelper
{
    public class DropDownHelper
    {
        private readonly BmsbtContext _context;

        public DropDownHelper(BmsbtContext context)
        {
            _context = context;
        }





        public List<SelectListItem> GetBanks()
        {
            // Filter configurations where ConfigKey equals "Bank"
            var BankName = _context.Configurations
                                   .Where(c => c.ConfigKey == "Bank")
                                   .ToList();

            // Map the filtered configurations to SelectListItem
            var Banks = BankName.Select(Bank => new SelectListItem
            {
                Value = Bank.ConfigKey.ToString(), 
                Text = Bank.ConfigValue.ToString()
            }).ToList();

            return Banks;
        }



        public List<SelectListItem> GetProjects()
        {
            // Filter configurations where ConfigKey equals "Bank"
            var Projects = _context.Configurations
                                   .Where(c => c.ConfigKey == "Lahore")
                                   .ToList();

            // Map the filtered configurations to SelectListItem
            var Project = Projects.Select(project => new SelectListItem
            {
                Value = project.ConfigValue.ToString(), // You may want to use ConfigValue or another property here if ConfigKey isn't the desired value
                Text = project.ConfigValue.ToString()
            }).ToList();

            return Project;
        }


        public List<SelectListItem> SuProjects(string projectId)
        {
            // Filter configurations where ConfigKey equals the selected ProjectId
            var SubProjects = _context.Configurations
                                     .Where(c => c.ConfigKey == projectId)  // Assuming ConfigKey is related to ProjectId
                                     .ToList();

            // Map the filtered configurations to SelectListItem
            var SubProject = SubProjects.Select(project => new SelectListItem
            {
                Value = project.ConfigValue.ToString(), // Use ConfigValue as the value for the subproject
                Text = project.ConfigValue.ToString()   // Use ConfigValue for display text (or another property)
            }).ToList();

            return SubProject;
        }


    }
}
