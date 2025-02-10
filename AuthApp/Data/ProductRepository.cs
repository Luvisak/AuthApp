using Microsoft.Data.SqlClient; 
using System.Data;               
using Dapper;
using AuthApp.Models;


namespace AuthApp.Data
{
    public class ProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM Products";
            var products = await connection.QueryAsync<Product>(sql);
            return products;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM Products WHERE Id = @Id";
            var product = await connection.QueryFirstOrDefaultAsync<Product>(sql, new { Id = id });
            return product;
        }

        public async Task<int> CreateAsync(Product product)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                INSERT INTO Products (Name, Price, Category)
                VALUES (@Name, @Price, @Category);
                SELECT CAST(SCOPE_IDENTITY() as int);
            ";

            var parameters = new
            {
                product.Name,
                product.Price,
                product.Category
            };

            var newId = await connection.ExecuteScalarAsync<int>(sql, parameters);
            return newId;
        }

        public async Task UpdateAsync(Product product)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                UPDATE Products
                SET Name = @Name,
                    Price = @Price,
                    Category = @Category
                WHERE Id = @Id;
            ";

            await connection.ExecuteAsync(sql, product);
        }

        public async Task DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "DELETE FROM Products WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = id });
        }
    }
}
