using RenewalWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Services
{
    public interface IUnsubscribeUserService
    {
        UnsubscribeUsers GetUnsubscribeUsersByEmail(string email);
        void Insert(UnsubscribeUsers unsubscribeUsers);
        void Update(UnsubscribeUsers unsubscribeUsers);
    }
}
