# Vocaluxe WebApp and webserver

Vocaluxe has a built-in webserver. Guests open the WebApp on their phones (scan the QR code shown under
`Options → Server`), add songs to the queue, manage playlists and profiles and use the phone as a remote.

| Folder              | What it is                                                                                     |
|---------------------|------------------------------------------------------------------------------------------------|
| `VocaluxeWeb`       | The webserver: ASP.NET Core 2.3 (Kestrel) on .NET Framework 4.8.1, REST API, live events       |
| `WebApp`            | The WebApp: Vue 3, TypeScript and Vite                                                         |
| `Vocaluxe/Base/Web` | The game side: starts the server (`CWebServer`) and implements `IVocaluxeRemote` (`CVocaluxeRemote`) |

## Architecture

* `VocaluxeWeb` does not reference the game. Everything it needs from Vocaluxe goes through the interface
  `IVocaluxeRemote`, which `CVocaluxeRemote` implements in the game.
* Requests run on Kestrel threads, but the game is not thread-safe. Calls into the game are queued with
  `CMainThreadDispatcher` and executed once per frame on the main thread (`CWebHost.Tick`, called from
  `CWebServer.ProcessTasks`). A call that is not executed within 5 seconds fails with `503`.
* The queue is a normal playlist in the game (default name `WebApp Queue`). `CQueueService` keeps who requested
  a song and who sings it. Songs are inserted fairly: the n-th song of a person goes behind everybody's n-th song.
  The queue can also be opened and started in the game (song menu → options → playlist group → WebApp queue).
* Live updates (queue, game state, notifications) are sent with Server-Sent Events (`GET /api/v1/events`).
* Logins are kept in memory (HttpOnly cookie `vlx_session`, logged out after 12 hours without use).
  Profiles without PIN log in again automatically after a restart of Vocaluxe.

### Roles and permissions

Every profile has a role: `Guest`, `Player` or `Admin`. Admins may do everything; what guests and players may do is set
in the WebApp under `More → Settings` (stored in `WebServer.json`). Admins can also

* see and log out logged-in devices,
* deactivate and delete profiles,
* see the address, port and HTTPS state of the server (the port is set in `config.xml` under `Server/ServerPort`, default 3000;
  HTTPS is switched in the game under `Options → Server`).

### Profile pictures

A profile picture is either an own photo (uploaded from the phone, saved as `WebApp_*.jpg` in the profile folder) or one of
the avatars that come with Vocaluxe or were put into the profile folder (`GET /api/v1/avatars`). Uploaded photos of other
people are not offered for selection.

### Files

Stored in the Vocaluxe data folder:

| File               | Content                                                       |
|--------------------|---------------------------------------------------------------|
| `WebServer.json`   | WebApp settings: permissions, queue playlist name             |
| `WebServer.pfx`    | Self-signed certificate for HTTPS (created on first use)      |
| `WebCache`         | Resized covers and avatars                                    |

The built WebApp is copied to the `Website` folder next to `Vocaluxe.exe`.

## Building

[Node.js](https://nodejs.org) (LTS, 22.12 or newer) is required. Building the project `VocaluxeWeb` (or the
whole solution) runs `npm ci` (only when `package.json` or `package-lock.json` changed) and `npm run build` in `WebApp`.
If Node.js is missing, the build stops with a hint. Restart Visual Studio after installing Node.js.

## Developing the WebApp

Start Vocaluxe with the webserver switched on, then:

```
cd WebApp
npm run dev
```

Vite serves the WebApp with hot reload and forwards `/api` and `/swagger` to Vocaluxe at `http://localhost:3000`.
For another address use `VOCALUXE_URL=http://192.168.0.10:3000 npm run dev`.

## REST API

* All endpoints are under `/api/v1`. Errors are returned as problem details (`{ "title": "...", "status": 403 }`).
* Swagger UI: `http://<address>:3000/swagger`, OpenAPI document: `/swagger/v1/swagger.json`.
* The TypeScript types of the WebApp (`WebApp/src/api/schema.d.ts`) are generated from the OpenAPI document.
  After changing the API, start Vocaluxe (or set `VOCALUXE_URL`) and run `npm run api:generate` in `WebApp`.

## HTTPS

HTTP is the default. With `Options → Server → Server encryption` switched on, Vocaluxe uses HTTPS with a self-signed
certificate for the computer's name and IP addresses. Browsers do not trust it and show a warning the first time
("Your connection is not private"); the connection is encrypted nevertheless. The WebApp explains this on its help page.
Some browser features (camera, installing the WebApp on the home screen) only work over HTTPS. With the
self-signed certificate they depend on the browser: some browsers allow them after accepting the warning, others don't.

## Translations

The texts of the WebApp are in `WebApp/src/locales/<language>.json` (flat key/value JSON, `en.json` is the source).
They are translated on [Transifex](https://www.transifex.com/projects/p/vocaluxe/) like the game
(see `transifex.yaml`, format `KEYVALUEJSON`). New languages are picked up automatically when a file is added.
Placeholders like `{name}` must stay unchanged.

## Tests

The NUnit tests in `Tests/VocaluxeWeb` cover PIN hashing, settings and permissions, the fair queue order and the
device names shown to admins. They run with the other tests of the `Tests` project.
