using HizzaCoinBackend.Controllers;
using HizzaCoinBackend.Models;
using HizzaCoinBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HizzaCoinBackendTests;

public class ControllerContractTests
{
    [Fact]
    public async Task AccountsController_obeys_crud_contract()
    {
        var service = new Mock<IAccountsService>();
        var existing = new Account("existing", 10, 0, DateTime.MinValue, 0) { Id = "found-id" };
        service.Setup(x => x.GetAsync()).ReturnsAsync([existing]);
        service.Setup(x => x.GetAsync(It.IsAny<string>()))
            .Returns((string id) => Task.FromResult<Account?>(id == "missing" ? null : existing));
        service.Setup(x => x.CreateAsync(It.IsAny<Account>()))
            .Callback<Account>(x => x.Id = "created-id").Returns(Task.CompletedTask);
        service.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Account>())).Returns(Task.CompletedTask);
        service.Setup(x => x.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        var controller = new AccountsController(service.Object);

        await AssertCrudContract(
            new CrudActions<Account>(
                controller.Get,
                controller.Get,
                controller.Post,
                controller.Update,
                controller.Delete,
                x => x.Id,
                (x, id) => x.Id = id),
            [existing],
            new Account("new", 0, 0, DateTime.MinValue, 0),
            new Account("updated", 20, 0, DateTime.MinValue, 0));
    }

    [Fact]
    public async Task ChallengesController_obeys_crud_contract()
    {
        var service = new Mock<IChallengesService>();
        var existing = NewChallenge("existing");
        existing.Id = "found-id";
        service.Setup(x => x.GetAsync()).ReturnsAsync([existing]);
        service.Setup(x => x.GetAsync(It.IsAny<string>()))
            .Returns((string id) => Task.FromResult<Challenge?>(id == "missing" ? null : existing));
        service.Setup(x => x.CreateAsync(It.IsAny<Challenge>()))
            .Callback<Challenge>(x => x.Id = "created-id").Returns(Task.CompletedTask);
        service.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Challenge>())).Returns(Task.CompletedTask);
        service.Setup(x => x.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        var controller = new ChallengesController(service.Object);

        await AssertCrudContract(
            new CrudActions<Challenge>(
                controller.Get,
                controller.Get,
                controller.Post,
                controller.Update,
                controller.Delete,
                x => x.Id,
                (x, id) => x.Id = id),
            [existing],
            NewChallenge("new"),
            NewChallenge("updated"));
    }

    [Fact]
    public async Task RewardsController_obeys_crud_contract()
    {
        var service = new Mock<IRewardsService>();
        var existing = new Reward { Id = "found-id", Streak = 2, RewardedAmount = 100 };
        service.Setup(x => x.GetAsync()).ReturnsAsync([existing]);
        service.Setup(x => x.GetAsync(It.IsAny<string>()))
            .Returns((string id) => Task.FromResult<Reward?>(id == "missing" ? null : existing));
        service.Setup(x => x.CreateAsync(It.IsAny<Reward>()))
            .Callback<Reward>(x => x.Id = "created-id").Returns(Task.CompletedTask);
        service.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Reward>())).Returns(Task.CompletedTask);
        service.Setup(x => x.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        var controller = new RewardsController(service.Object);

        await AssertCrudContract(
            new CrudActions<Reward>(
                controller.Get,
                controller.Get,
                controller.Post,
                controller.Update,
                controller.Delete,
                x => x.Id,
                (x, id) => x.Id = id),
            [existing],
            new Reward { Streak = 3, RewardedAmount = 200 },
            new Reward { Streak = 4, RewardedAmount = 300 });
    }

    [Fact]
    public async Task RouletteController_obeys_crud_contract()
    {
        var service = new Mock<IRouletteService>();
        var existing = new Roulette(4, 4, RouletteType.Number) { Id = "found-id" };
        service.Setup(x => x.GetAsync()).ReturnsAsync([existing]);
        service.Setup(x => x.GetAsync(It.IsAny<string>()))
            .Returns((string id) => Task.FromResult<Roulette?>(id == "missing" ? null : existing));
        service.Setup(x => x.CreateAsync(It.IsAny<Roulette>()))
            .Callback<Roulette>(x => x.Id = "created-id").Returns(Task.CompletedTask);
        service.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Roulette>())).Returns(Task.CompletedTask);
        service.Setup(x => x.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        var controller = new RouletteController(service.Object);

        await AssertCrudContract(
            new CrudActions<Roulette>(
                controller.Get,
                controller.Get,
                controller.Post,
                controller.Update,
                controller.Delete,
                x => x.Id,
                (x, id) => x.Id = id),
            [existing],
            new Roulette(5, 1, RouletteType.Colour),
            new Roulette(6, 2, RouletteType.Colour));
    }

    [Fact]
    public async Task TransactionsController_obeys_crud_contract()
    {
        var service = new Mock<ITransactionsService>();
        var existing = NewTransaction("existing");
        existing.Id = "found-id";
        service.Setup(x => x.GetAsync()).ReturnsAsync([existing]);
        service.Setup(x => x.GetAsync(It.IsAny<string>()))
            .Returns((string id) => Task.FromResult<Transaction?>(id == "missing" ? null : existing));
        service.Setup(x => x.CreateAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(x => x.Id = "created-id").Returns(Task.CompletedTask);
        service.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        service.Setup(x => x.RemoveAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        var controller = new TransactionsController(service.Object);

        await AssertCrudContract(
            new CrudActions<Transaction>(
                controller.Get,
                controller.Get,
                controller.Post,
                controller.Update,
                controller.Delete,
                x => x.Id,
                (x, id) => x.Id = id),
            [existing],
            NewTransaction("new"),
            NewTransaction("updated"));
    }

    [Fact]
    public async Task StatisticsController_returns_service_result()
    {
        var expected = new HizzaCoinBackend.Models.DTOs.StatisticsResponse(1, 2, 3, 4, 5, 6, 7, 8);
        var service = new Mock<IStatisticsService>();
        service.Setup(x => x.GetStats()).ReturnsAsync(expected);

        var result = await new StatisticsController(service.Object).GetStatistics();

        Assert.Same(expected, result.Value);
    }

    private static async Task AssertCrudContract<TEntity>(
        CrudActions<TEntity> actions,
        List<TEntity> expectedList,
        TEntity newEntity,
        TEntity updatedEntity)
    {
        Assert.Equal(expectedList, await actions.GetAll());

        Assert.IsType<NotFoundResult>((await actions.GetById("missing")).Result);
        Assert.Equal(expectedList[0], (await actions.GetById("found")).Value);

        var createResult = Assert.IsType<CreatedAtActionResult>(await actions.Create(newEntity));
        Assert.Equal("created-id", createResult.RouteValues!["id"]);
        Assert.Equal(newEntity, createResult.Value);

        Assert.IsType<NotFoundResult>(await actions.Update("missing", updatedEntity));
        actions.SetId(updatedEntity, "request-id");
        var updateResult = Assert.IsType<NoContentResult>(await actions.Update("found", updatedEntity));
        Assert.Equal(204, updateResult.StatusCode);
        Assert.Equal("found-id", actions.GetId(updatedEntity));

        Assert.IsType<NotFoundResult>(await actions.Delete("missing"));
        Assert.IsType<NoContentResult>(await actions.Delete("found"));
    }

    private static Challenge NewChallenge(string discordId) =>
        new(discordId, "opponent", 0, DateTime.MinValue, Hand.NotSelected, Hand.NotSelected, ChallengeState.InProgress);

    private static Transaction NewTransaction(string discordId) =>
        new(discordId, "recipient", 10, DateTime.MinValue, TransactionType.Give);

    private sealed record CrudActions<TEntity>(
        Func<Task<List<TEntity>>> GetAll,
        Func<string, Task<ActionResult<TEntity>>> GetById,
        Func<TEntity, Task<IActionResult>> Create,
        Func<string, TEntity, Task<IActionResult>> Update,
        Func<string, Task<IActionResult>> Delete,
        Func<TEntity, string?> GetId,
        Action<TEntity, string?> SetId);
}
