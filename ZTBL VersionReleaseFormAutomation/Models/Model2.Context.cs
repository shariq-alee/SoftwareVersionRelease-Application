﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ZTBL_VersionReleaseFormAutomation.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class DatabaseZTBLEntities : DbContext
    {
        public DatabaseZTBLEntities()
            : base("name=DatabaseZTBLEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<BugReport> BugReports { get; set; }
        public virtual DbSet<BugReportTable> BugReportTables { get; set; }
        public virtual DbSet<FormData> FormDatas { get; set; }
        public virtual DbSet<Project> Projects { get; set; }
        public virtual DbSet<ReleasedFilesList> ReleasedFilesLists { get; set; }
        public virtual DbSet<SoftwareFeatureList> SoftwareFeatureLists { get; set; }
        public virtual DbSet<UAT_Team> UAT_Teams { get; set; }
        public virtual DbSet<UAT_Team1> UAT_Teams1 { get; set; }
        public virtual DbSet<UATForm> UATForms { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Form> Forms { get; set; }
    }
}
