namespace WebAppSystems.Models
{
    // Vínculo N:N entre um gestor (Attorney com IsGestor=true) e as áreas (Department) que ele acompanha.
    // Um gestor pode cobrir várias áreas; uma área pode ter vários gestores.
    public class GestorArea
    {
        public int Id { get; set; }

        public int AttorneyId { get; set; }
        public Attorney? Attorney { get; set; }

        public int DepartmentId { get; set; }
        public Department? Department { get; set; }

        public GestorArea()
        {
        }

        public GestorArea(int attorneyId, int departmentId)
        {
            AttorneyId = attorneyId;
            DepartmentId = departmentId;
        }
    }
}
