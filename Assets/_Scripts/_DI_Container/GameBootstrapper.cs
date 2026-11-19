using Assets._Scripts._Core.Services;
using System;
using VContainer.Unity;

namespace Assets._Scripts._DI_Container
{
    public class GameBootstrapper : IStartable, ITickable, IDisposable
    {
        private readonly SteamSessionService _steamSessionService;


        public GameBootstrapper(
            SteamSessionService steamSessionService
            )
        {
            _steamSessionService = steamSessionService;
        }


        public void Start()
        {
            _steamSessionService.Initialize();
        }

        public void Tick()
        {
            _steamSessionService.Tick();
        }

        void IDisposable.Dispose()
        {
            _steamSessionService.Dispose();
        }
    }
}
