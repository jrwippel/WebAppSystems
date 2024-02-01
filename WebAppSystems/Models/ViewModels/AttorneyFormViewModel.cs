namespace WebAppSystems.Models.ViewModels
{
    public class AttorneyFormViewModel
    {
        public Attorney Attorney { get; set; }         
        public ICollection<Department> Departments { get; set; }    
       
    }
}
