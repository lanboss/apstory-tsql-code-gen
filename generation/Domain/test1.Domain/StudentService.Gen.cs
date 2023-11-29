using System;
using test1.Dal.Interface;
using test1.Domain.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace test1.Domain
{
    public partial class StudentService : IStudentService
    {
        private readonly IStudentRepository _repo;
        public StudentService(IStudentRepository repo)
        {
            _repo = repo;
        }
        public async Task<test1.Model.Student> InsUpdStudent(test1.Model.Student student)
        {
            return await _repo.InsUpdStudent(student);
        }
        public async Task<List<test1.Model.Student>> GetStudentByI)
        {
            return await _repo.GetStudentByI);
        }
    }
}