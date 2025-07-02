using HizzaCoinBackend.Models;
using HizzaCoinBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace HizzaCoinBackend.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly AccountsService _accountsService;
    
    public AccountsController(AccountsService accountsService) =>
        _accountsService = accountsService;

    [HttpGet]
    public async Task<List<Account>> Get() =>
        await _accountsService.GetAsync();

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Account>> Get(string id)
    {
        var account = await _accountsService.GetAsync(id);

        if (account is null)
        {
            return NotFound();
        }

        return account;
    }
    
    [HttpPost]
    public async Task<IActionResult> Post(Account newAccount)
    {
        await _accountsService.CreateAsync(newAccount);

        return CreatedAtAction(nameof(Get), new { id = newAccount.Id }, newAccount);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Update(string id, Account updatedAccount)
    {
        var account = await _accountsService.GetAsync(id);

        if (account is null)
        {
            return NotFound();
        }

        updatedAccount.Id = account.Id;

        await _accountsService.UpdateAsync(id, updatedAccount);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var account = await _accountsService.GetAsync(id);

        if (account is null)
        {
            return NotFound();
        }

        await _accountsService.RemoveAsync(id);

        return NoContent();
    }
}