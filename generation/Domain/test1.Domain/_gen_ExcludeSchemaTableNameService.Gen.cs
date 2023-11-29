using System;
using test1.Dal.Interface;
using test1.Domain.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace test1.Domain
{
    public partial class *gen_ExcludeSchemaTableNameService : I*gen_ExcludeSchemaTableNameService
    {
        private readonly I*gen_ExcludeSchemaTableNameRepository _repo;
        public *gen_ExcludeSchemaTableNameService(I*gen_ExcludeSchemaTableNameRepository repo)
        {
            _repo = repo;
        }
        public async Task<test1.Model.*gen_ExcludeSchemaTableName> InsUpd*gen_ExcludeSchemaTableName(test1.Model.*gen_ExcludeSchemaTableName *gen_ExcludeSchemaTableName)
        {
            return await _repo.InsUpd*gen_ExcludeSchemaTableName(*gen_ExcludeSchemaTableName);
        }
        public async Task<List<test1.Model.*gen_ExcludeSchemaTableName>> Get*gen_ExcludeSchemaTableNameByI)
        {
            return await _repo.Get*gen_ExcludeSchemaTableNameByI);
        }
    }
}