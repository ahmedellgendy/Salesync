using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Domain.Common.Enums.Sales;
using Xunit;
namespace Salesync.Tests;
public class PreviousSessionTests
{
    [Fact] public async Task Today_surfaces_old_open_session_and_new_day_stays_blocked_until_it_is_closed()
    {
        await using var f=await WorkflowFixture.CreateAsync();
        var day=f.Get<IBusinessClock>().Today;
        var old=await f.SessionAsync(); old.WorkingDate=day.AddDays(-3); await f.Db.SaveChangesAsync();
        var mobile=f.Get<ISalesRepMobileService>();
        var today=await mobile.GetTodayAsync();
        Assert.True(today.HasUnclosedPreviousSession); Assert.True(today.HasOpenSession);
        Assert.False(today.HasTodaySession); Assert.False(today.IsDayClosed); Assert.Equal(old.Id,today.Session!.Id);
        Assert.Equal(day.AddDays(-3),today.Session.WorkingDate);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Get<ISalesRepSessionService>().StartSessionAsync(new(){SalesRepId=1}));
        Assert.Equal(1,await f.Db.SalesRepSessions.CountAsync());
        await mobile.CloseDayAsync(old.Id,1);
        today=await mobile.GetTodayAsync(); Assert.False(today.HasOpenSession); Assert.False(today.HasUnclosedPreviousSession); Assert.Null(today.Session);
        await mobile.StartDayAsync(new());
        today=await mobile.GetTodayAsync(); Assert.True(today.HasTodaySession); Assert.True(today.HasOpenSession); Assert.False(today.HasUnclosedPreviousSession);
        Assert.Equal(day,today.Session!.WorkingDate);
    }
    [Fact] public async Task Closed_prior_day_is_not_reported_as_open_and_other_representative_sessions_are_excluded()
    {
        await using var f=await WorkflowFixture.CreateAsync(); var own=await f.SessionAsync(); own.Status=DayStatus.Closed; own.EndTime=DateTime.UtcNow;
        await f.Db.SaveChangesAsync(); await f.SessionAsync(2,2);
        var today=await f.Get<ISalesRepMobileService>().GetTodayAsync();
        Assert.False(today.HasOpenSession); Assert.False(today.HasUnclosedPreviousSession); Assert.Null(today.Session);
    }
    [Fact] public async Task Current_open_day_is_prioritized_and_prior_open_day_surfaces_after_closing_it()
    {
        await using var f=await WorkflowFixture.CreateAsync(); var day=f.Get<IBusinessClock>().Today;
        var oldest=await f.SessionAsync(1); oldest.WorkingDate=day.AddDays(-5);
        var current=await f.SessionAsync(2); current.WorkingDate=day; current.StartTime=DateTime.UtcNow;
        await f.Db.SaveChangesAsync();
        var today=await f.Get<ISalesRepMobileService>().GetTodayAsync();
        Assert.True(today.HasTodaySession); Assert.False(today.HasUnclosedPreviousSession); Assert.Equal(2,today.Session!.Id);
        await f.Get<ISalesRepMobileService>().CloseDayAsync(2,1);
        today=await f.Get<ISalesRepMobileService>().GetTodayAsync();
        Assert.True(today.HasUnclosedPreviousSession); Assert.Equal(1,today.Session!.Id);
    }
}
