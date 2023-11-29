using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace test1.Dal.Interface
{
    public partial interface IStudentRepository
    {
        Task<test1.Model.Student> InsUpdStudent(test1.Model.Student student);
        Task<List<test1.Model.Student>> GetStudentByI);
        Task DelStudentHr);
    }
}