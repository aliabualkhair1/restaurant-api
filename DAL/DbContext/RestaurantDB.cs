using DAL.Entities.AppUser;
using DAL.Entities.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DbContext
{
    public class RestaurantDB : IdentityDbContext<ApplicationUser>
    {
        public RestaurantDB(DbContextOptions<RestaurantDB> options):base(options)
        {
        }
        public DbSet<Menu> Menu { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<MenuItems> MenuItems { get; set; }
        public DbSet<OrderItems> OrderItems { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Tables> Tables { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<RefreshToken> RefreshToken { get; set; }
        public DbSet<OrderFeedback> OrdersFeedBack { get; set; }
        public DbSet<ReservationFeedback> ReservationsFeedBack { get; set; }
        public DbSet<ComplaintandSuggestion> ComplaintandSuggestion { get; set; }

    }

}
