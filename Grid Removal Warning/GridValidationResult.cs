using System.Collections.Generic;

namespace Grid_Removal_Warning
{
    public class GridValidationResult
    {
        public GridInfo Grid { get; set; }// Store the grid information being validated

        public List<string> Problems { get; } = new List<string>(); // Store a list of problems found during validation for reporting or further analysis

        public bool HasProblems => Problems.Count > 0; // Indicate whether any problems were found during validation for quick checks in the code
    }
}