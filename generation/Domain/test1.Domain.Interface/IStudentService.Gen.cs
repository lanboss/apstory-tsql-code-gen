using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace test1.Domain.Interface
{
    public partial interface IStudentService
    {
        Task<test1.Model.Student> InsUpdStudent(test1.Model.Student student);
        Task<List<test1.Model.Student>> GetStudentByI);
        Task DelStudentHr);
    }
}