namespace WebAppSystems.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } 
  
        public ICollection<Attorney> Attorneys { get; set; } = new List<Attorney>();
        public ICollection<ProcessRecord> ProcessRecords { get; set; } = new List<ProcessRecord>();
        //public ICollection<MensalistaDepartment> MensalistaDepartments { get; set; } = new List<MensalistaDepartment>();

        public Department()
        {
        }

        public Department(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public Department(string name)
        {
            Name = name;
        }

        public void AddAtorney(Attorney attorney)
        {
            Attorneys.Add(attorney);
        }

        //    public TimeSpan TotalHours(DateTime initial, DateTime final)
        //   {
        //      return Attorneys.Sum(attorney => attorney.TotalHours(initial, final));
        //  }
        /*
          public TimeSpan TotalHours(DateTime initial, DateTime final)
          {
              TimeSpan total = TimeSpan.Zero;
              foreach (var attorney in Attorneys)
              {
                  total += attorney.TotalHours(initial, final);
              }        
              return total;         
          }  
        */

        public string TotalHours(DateTime initial, DateTime final)
        {
            var total = TimeSpan.Zero;
            foreach (var attorney in Attorneys)
            {
                total += attorney.TotalHours(initial, final);
            }

            string formattedTotal = $"{(int)total.TotalHours}:{total.Minutes:00}";
            return formattedTotal;
        }
    }
}
