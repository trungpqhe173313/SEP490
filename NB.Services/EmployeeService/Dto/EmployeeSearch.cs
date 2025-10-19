using NB.Service.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Repository.EmployeeRepository.Dto
{
    public class EmployeeSearch : SearchBase
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
