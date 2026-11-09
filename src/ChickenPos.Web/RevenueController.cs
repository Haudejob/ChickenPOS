

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ChickenPos.Web.Controllers
{
    [Authorize(Policy = "manager")]
    [ApiController]
    [Route("api/[controller]")]
    public class RevenueController : ControllerBase
    {
        private readonly Store _store;

        public RevenueController(Store store)
        {
            _store = store;
        }

        // Tổng quan doanh thu hôm nay
        [HttpGet("summary")]
        public async Task<IActionResult> GetRevenueSummary()
        {
            var today = DateTime.UtcNow.AddHours(7).Date;

            var rows = await _store.Read(
                @"SELECT
                    COUNT(*) AS totalOrders,
                    COALESCE(SUM(Amount), 0) AS totalRevenue
                  FROM Payment
                  WHERE CAST(DATEADD(HOUR, 7, PaidAt) AS date) = @today",
                ("today", today)
            );

            var totalOrders = Convert.ToInt32(rows[0]["totalOrders"]);
            var totalRevenue = Convert.ToDecimal(rows[0]["totalRevenue"]);

            var averageOrderValue = totalOrders > 0
                ? totalRevenue / totalOrders
                : 0m;

            return Ok(new RevenueSummaryResponse(
                totalOrders,
                totalRevenue,
                averageOrderValue,
                today.ToString("yyyy-MM-dd")
            ));
        }

        // Báo cáo doanh thu 7 ngày gần nhất
        [HttpGet("daily-report")]
        public async Task<IActionResult> GetDailyReport()
        {
            var today = DateTime.UtcNow.AddHours(7).Date;
            var startDate = today.AddDays(-6);
            var endDate = today.AddDays(1);

            var rows = await _store.Read(
                @"SELECT
                    CONVERT(varchar(10),
                        CAST(DATEADD(HOUR, 7, PaidAt) AS date), 23) AS reportDate,
                    COUNT(*) AS ordersCount,
                    COALESCE(SUM(Amount), 0) AS totalAmount
                  FROM Payment
                  WHERE CAST(DATEADD(HOUR, 7, PaidAt) AS date) >= @startDate
                    AND CAST(DATEADD(HOUR, 7, PaidAt) AS date) < @endDate
                  GROUP BY CAST(DATEADD(HOUR, 7, PaidAt) AS date)
                  ORDER BY CAST(DATEADD(HOUR, 7, PaidAt) AS date)",
                ("startDate", startDate),
                ("endDate", endDate)
            );

            var reportList = rows.Select(r => new RevenueDailyReportRow(
                r["reportDate"]?.ToString() ?? string.Empty,
                Convert.ToInt32(r["ordersCount"]),
                Convert.ToDecimal(r["totalAmount"])
            )).ToList();

            return Ok(reportList);
        }
    }

    public sealed record RevenueSummaryResponse(int totalOrders, decimal totalRevenue, decimal averageOrderValue, string date);
    public sealed record RevenueDailyReportRow(string date, int ordersCount, decimal totalAmount);
}
