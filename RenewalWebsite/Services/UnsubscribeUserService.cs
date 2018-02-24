using Microsoft.EntityFrameworkCore;
using RenewalWebsite.Data;
using RenewalWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RenewalWebsite.Services
{
    public class UnsubscribeUserService : IUnsubscribeUserService
    {
        private readonly ApplicationDbContext _dbContext;

        public UnsubscribeUserService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Insert(UnsubscribeUsers unsubscribeUsers)
        {
            _dbContext.UnsubscribeUsers.Add(unsubscribeUsers);
            _dbContext.SaveChanges();
        }

        public void Update(UnsubscribeUsers unsubscribeUsers)
        {
            _dbContext.Entry(unsubscribeUsers).State = EntityState.Modified;
            _dbContext.SaveChanges();
        }

        public UnsubscribeUsers GetUnsubscribeUsersByEmail(string email)
        {
            return _dbContext.UnsubscribeUsers.Where(a=>a.email.ToLower().Equals(email.ToLower())).FirstOrDefault();
        }
    }
}
