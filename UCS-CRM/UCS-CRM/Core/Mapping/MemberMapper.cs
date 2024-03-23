namespace UCS_CRM.Core.Mapping
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UCS_CRM.Core.Models;

    public static class MemberMapper
    {
        public static Member MapToMember(Datum datum)
        {
            if (datum == null)
                return null;

            return new Member
            {
                Fidxno = Convert.ToInt32(datum.FIdxno),
                FirstName = datum.FirstName,
                LastName = datum.LastName,
                DateOfBirth = datum.Dob.DateTime,
                AccountNumber = datum.Account,
                NationalId = datum.Idno,
                PhoneNumber = datum.Mobile,
                Branch = datum.Branch,
                Employer = datum.Employer,
               
            };
        }

  
    
        public static List<Member> MapToMembers(List<Datum> data)
        {
            return data.Select(MapToMember).ToList();
        }
    }
}
