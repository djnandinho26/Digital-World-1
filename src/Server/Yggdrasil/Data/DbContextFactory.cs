using System;
using Microsoft.EntityFrameworkCore;

namespace Digital_World.Data
{
    /// <summary>
    /// Factory para criar instâncias do DbContext
    /// </summary>
    public static class DbContextFactory
    {
        private static string _connectionString = string.Empty;
        private static bool _isInitialized = false;

        public static void Initialize(string host, string user, string password, string database)
        {
            _connectionString = $"Server={host};User ID={user};Password={password};Database={database};";
            _isInitialized = true;
        }

        public static bool IsInitialized => _isInitialized;

        public static DigitalWorldContext CreateDbContext()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("DbContext não foi inicializado. Chame DbContextFactory.Initialize() primeiro.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<DigitalWorldContext>();
            optionsBuilder.UseMySql(
                _connectionString,
                ServerVersion.AutoDetect(_connectionString),
                options => options.EnableRetryOnFailure()
            );

            return new DigitalWorldContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Testa a conexão com o banco de dados
        /// </summary>
        public static bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                errorMessage = "Connection string não configurada";
                return false;
            }

            try
            {
                using var context = CreateDbContext();
                context.Database.CanConnect();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}
