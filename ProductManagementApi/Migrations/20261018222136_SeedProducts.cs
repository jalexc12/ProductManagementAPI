using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductManagementApi.Migrations
{
    /// <inheritdoc />
    public partial class SeedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO [Products] ([Name], [Description], [Price], [Stock], [CreatedAt])
                VALUES
                    ('ZEISS Camera Lens', 'Seed data - standard camera lens', 499.99, 12, SYSUTCDATETIME()),
                    ('ZEISS Binoculars', 'Seed data - compact binoculars', 329.50, 0, SYSUTCDATETIME()),
                    ('ZEISS Cleaning Kit', 'Seed data - lens cleaning kit', 24.95, 25, SYSUTCDATETIME()),
                    ('ZEISS Tripod', 'Seed data - lightweight travel tripod', 149.00, 8, SYSUTCDATETIME()),
                    ('ZEISS Telescope', 'Seed data - amateur astronomy telescope', 899.00, 3, SYSUTCDATETIME()),
                    ('ZEISS Reading Glasses', 'Seed data - reading glasses', 89.99, 16, SYSUTCDATETIME()),
                    ('ZEISS Lens Wipes', 'Seed data - disposable lens wipes', 9.50, 40, SYSUTCDATETIME()),
                    ('ZEISS Digital Camera', 'Seed data - compact digital camera', 649.00, 5, SYSUTCDATETIME());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM [Products]
                WHERE [Description] IN (
                    'Seed data - standard camera lens',
                    'Seed data - compact binoculars',
                    'Seed data - lens cleaning kit',
                    'Seed data - lightweight travel tripod',
                    'Seed data - amateur astronomy telescope',
                    'Seed data - reading glasses',
                    'Seed data - disposable lens wipes',
                    'Seed data - compact digital camera'
                );
                """);
        }
    }
}
