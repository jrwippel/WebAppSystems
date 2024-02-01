using NuGet.Protocol.Plugins;
using Org.BouncyCastle.Crypto.Agreement.JPake;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;

namespace WebAppSystems.Data
{
    public class SeedingService
    {

        private WebAppSystemsContext _context;
        public SeedingService(WebAppSystemsContext context)
        {
            _context = context;     
        }
        

        public void Seed()
        {
            if (_context.ProcessRecord.Any() ||
                _context.Department.Any() ||
                _context.Attorney.Any() ||
                _context.PrecoCliente.Any())              
                
            {
                return;
            }

            /*
            Department d1 = new Department("Trabalhista");
            Department d2 = new Department("Criminal");
            Department d3 = new Department("Tributária");
            Department d4 = new Department("Cívil");        


            Attorney a1 = new Attorney("Jach Reacher", "jack.reacher@cia.com", "47 9 99346159", new DateTime(1998, 4, 21),d1,ProfileEnum.Padrao, "7c4a8d09ca3762af61e59520943dc26494f8941b", new DateTime(1998, 4, 21),new DateTime(1998, 4, 21), "jackson.reacher");            

            Attorney a2 = new Attorney("Jason Burn", "jason.burn@treadstone.com", "47 9 99346144", new DateTime(1979, 12, 31), d2, ProfileEnum.Padrao, "7c4a8d09ca3762af61e59520943dc26494f8941b", new DateTime(1998, 4, 21), new DateTime(1998, 4, 21), "jason.burne");
            Attorney a3 = new Attorney("Ethan Hunt", "ethan.hunt@mi6.com", "47 9 99346133", new DateTime(1988, 1, 15), d1, ProfileEnum.Padrao, "7c4a8d09ca3762af61e59520943dc26494f8941b", new DateTime(1998, 4, 21), new DateTime(1998, 4, 21), "ethan.hunt");
            Attorney a4 = new Attorney("Grace Hoper", "grace.hoper@pentagono.gov", "47 9 99346123", new DateTime(1993, 11, 30), d4, ProfileEnum.Padrao, "7c4a8d09ca3762af61e59520943dc26494f8941b", new DateTime(1998, 4, 21), new DateTime(1998, 4, 21), "grace.hoper");
            Attorney a5 = new Attorney("Jack Ryan", "jack.ryan@cia.com", "47 9 99346789", new DateTime(2000, 1, 9), d3, ProfileEnum.Padrao, "7c4a8d09ca3762af61e59520943dc26494f8941b", new DateTime(1998, 4, 21), new DateTime(1998, 4, 21), "jack.ryan");
            Attorney a6 = new Attorney("Jackson R Wippel", "jackson.wippel@abin.com", "47 9 99346765", new DateTime(1997, 3, 4), d2, ProfileEnum.Admin, "7c4a8d09ca3762af61e59520943dc26494f8941b", new DateTime(1998, 4, 21), new DateTime(1998, 4, 21), "jrwippel");
            Attorney a7 = new Attorney("James Bond", "james.bond@mi.com", "47 9 99346765", new DateTime(1997, 3, 4), d2, ProfileEnum.Padrao, "7c4a8d09ca3762af61e59520943dc26494f8941b", new DateTime(1998, 4, 21), new DateTime(1998, 4, 21), "james.bond");

            Client c1 = new Client("Google SA", "84.148.436.0001-12", "google@gmail.com","4721235401");
            Client c2 = new Client("Jrw Soluções em TI", "50.456.126.0001-76", "jrwippel@hotmail.com", "4799346159");

            TimeSpan horaini = new TimeSpan(2, 30, 0);
            TimeSpan horafin = new TimeSpan(3, 50, 0);

            
            ProcessRecord r1 = new ProcessRecord(new DateTime(2018, 09, 25), a1, c1, horaini, horafin, "Viajem a Argentina para identificar possivel fraude", d1, "Daniel");
            ProcessRecord r2 = new ProcessRecord(new DateTime(2018, 09, 4), a5, c1, horaini, horafin, "Reunião com chefe do setor de armas quimicas da Russia", d2, "Maria Eduarda");
            ProcessRecord r3 = new ProcessRecord(new DateTime(2018, 09, 13), a4, c2, horaini, horafin, "Apoio ao serviço secreto Brasileiro", d3, "Denilson Silveira");
            ProcessRecord r4 = new ProcessRecord(new DateTime(2018, 09, 1), a2, c1, horaini, horafin, "Analise de possível relatório do time de contra-terrorimos", d4, "João");            
            ProcessRecord r6 = new ProcessRecord(new DateTime(2018, 10, 10), a7, c2, horaini, horafin, "Criação de equipe de apoio", d2, "Joel");
            ProcessRecord r7 = new ProcessRecord(new DateTime(2018, 10, 10), a6, c2, horaini, horafin, "Viajem a strasburgo na França", d2, "Maria Joaquina");

            PrecoCliente pc1 = new PrecoCliente(c1, d1, 150.22);
            PrecoCliente pc2 = new PrecoCliente(c1, d2, 150.33);
            PrecoCliente pc3 = new PrecoCliente(c1, d3, 150.44);
            PrecoCliente pc4 = new PrecoCliente(c1, d4, 150.55);

            PrecoCliente pc5 = new PrecoCliente(c2, d1, 250.22);
            PrecoCliente pc6 = new PrecoCliente(c2, d2, 250.33);
            PrecoCliente pc7 = new PrecoCliente(c2, d3, 250.44);
            PrecoCliente pc8 = new PrecoCliente(c2, d4, 250.55);           


            _context.Department.AddRange(d1, d2, d3, d4);

            _context.Attorney.AddRange(a1, a2, a3, a4, a5, a6);
            _context.Client.AddRange(c1, c2);

            _context.ProcessRecord.AddRange(
                r1, r2, r3, r4, r6, r7);
            _context.PrecoCliente.AddRange(pc1, pc2, pc3, pc4, pc5, pc6, pc7, pc8); 
            */

            Attorney a1 = new Attorney("Administrador", "jrwippel@hotmail.com", "47 9 99346159", new DateTime(1998, 4, 21), new Department("Infra"), ProfileEnum.Admin, "7c4a8d09ca3762af61e59520943dc26494f8941b", new DateTime(1998, 4, 21), new DateTime(1998, 4, 21), "admin");
            _context.Attorney.AddRange(a1);

            _context.SaveChanges();

        }


    }
}
