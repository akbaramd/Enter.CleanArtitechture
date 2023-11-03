﻿using ECA.Application.Abstractions.Commands.Users;
using ECA.Application.Abstractions.Services.Security;
using ECA.Application.Core;
using ECA.Application.Futures.Users.Models;
using ECA.Domain.AggregateModels.UserAggregates;
using ECA.Domain.Futures.Users.Exceptions;
using MapsterMapper;
using Optimum.SharedKernel.CQRS;

namespace ECA.Application.Futures.Users.Handlers.Commands;

public class AuthLoginCommandHandler : ICommandHandler<AuthLoginCommand, ServiceResult<AuthJwtDto>>
{
    private readonly IMapper _mapper;
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public AuthLoginCommandHandler(IUserRepository userRepository, 
        IMapper mapper, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _jwtService = jwtService;
    }

    public async Task<ServiceResult<AuthJwtDto>> Handle(AuthLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindOne<Guid>(x => x.Username == request.Username, cancellationToken);

        if (user == null) throw new AuthenticationFailedException();

        if (!user.ComparePassword(request.Password))
        {
            throw new AuthenticationFailedException();   
        }
        user.NewRefreshToken();

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
        
        var token = _jwtService.GenerateToken(user.Id.ToString(),new []{"Administrator"});
        var userDto = _mapper.Map<UserDto>(user);
        return new ServiceResult<AuthJwtDto>(new AuthJwtDto(token,user.RefreshToken,userDto));
    }
}