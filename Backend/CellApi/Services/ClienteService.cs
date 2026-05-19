using AutoMapper;
using CellApi.DTOs;
using CellApi.Models;
using CellApi.Repositories;

namespace CellApi.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _repo;
    private readonly IMapper _mapper;

    public ClienteService(IClienteRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ClienteDto>> GetAllAsync(bool soloActivos = true)
    {
        var clientes = await _repo.GetAllAsync(soloActivos);
        return _mapper.Map<IEnumerable<ClienteDto>>(clientes);
    }

    public async Task<ClienteDto?> GetByIdAsync(int id)
    {
        var cliente = await _repo.GetByIdAsync(id);
        return cliente == null ? null : _mapper.Map<ClienteDto>(cliente);
    }

    public async Task<ClienteDto> CreateAsync(CreateClienteDto dto)
    {
        if (await _repo.EmailExistsAsync(dto.Email))
            throw new InvalidOperationException($"Ya existe un cliente con el email '{dto.Email}'.");

        var cliente = _mapper.Map<Cliente>(dto);
        cliente.Activo = true;

        var id = await _repo.CreateAsync(cliente);
        cliente.Id = id;

        return _mapper.Map<ClienteDto>(cliente);
    }

    public async Task<ClienteDto> UpdateAsync(int id, UpdateClienteDto dto)
    {
        var existente = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Cliente {id} no encontrado.");

        if (await _repo.EmailExistsAsync(dto.Email, id))
            throw new InvalidOperationException($"El email '{dto.Email}' ya está en uso por otro cliente.");

        _mapper.Map(dto, existente);
        existente.Id = id;

        await _repo.UpdateAsync(existente);
        return _mapper.Map<ClienteDto>(existente);
    }

    public async Task DeleteAsync(int id)
    {
        var existente = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Cliente {id} no encontrado.");

        // Soft delete
        existente.Activo = false;
        await _repo.UpdateAsync(existente);
    }
}
