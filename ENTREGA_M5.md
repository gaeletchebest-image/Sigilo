# Entrega M5 — El último turno

## Ejecutar

1. En Windows, abrí `Builds/M5/ElUltimoTurno.exe`. Mantené la carpeta `ElUltimoTurno_Data` junto al ejecutable.
2. En el inicio, seleccioná **Jugar** o pulsá `Enter`/`Espacio`.
3. Recuperá la carpeta dorada del archivo central y alcanzá la plataforma verde de salida. Podés usar la ruta entre estanterías, más cubierta, o el pasillo abierto, más directo.
4. Observá los indicadores de ambos guardias. Si empieza una persecución, entrá al armario y esperá que el guardia investigue y vuelva a patrullar antes de salir.
5. El panel de servicio activa el zumbador cuando está disponible. No interrumpe una persecución activa.

## Controles

| Acción | Control |
| --- | --- |
| Moverse | `W`, `A`, `S`, `D` |
| Mirar | Mouse |
| Interactuar, recoger, entrar/salir del armario | `E` |
| Pausar / reanudar | `Esc` |
| Jugar desde el inicio | Botón **Jugar**, `Enter` o `Espacio` |
| Reiniciar tras pausa o resultado | Botón en pantalla o `R` |

El reinicio carga una tentativa nueva en el menú inicial. El cursor queda libre en menús y se captura durante la partida.

## Auditoría de la consigna

Referencia: `TSGD-TP2.pdf`. La consigna de nota 7 refina la derrota de nota 4: el jugador entra en persecución al ser detectado y pierde cuando el enemigo lo alcanza. El prototipo sigue esa condición refinada.

### Nota 4 — condiciones básicas

| Requisito | Sistema | Prueba y resultado |
| --- | --- | --- |
| Movimiento con físicas | `PlayerScr`, `Rigidbody`, `ControlsController` | Play Mode: recorridos por waypoints de NavMesh con físicas y colisiones activas. Se inició el ejecutable Windows x64; los controles externos quedan pendientes porque la ventana del proceso no estuvo accesible a la sesión de escritorio automatizada. |
| Enemigo patrullando entre puntos | `GuardNavigation`, `NavMeshAgent`; guardias A y B | Play Mode: dos guardias activos, con circuitos de varios waypoints; escena validada con cero referencias rotas. |
| Raycast con distancia y obstáculos | `GuardPerception` | T44, Play Mode: A vio al jugador con línea clara; al activar `RouteDivider`, no vio a igual posición, distancia y ángulo. |
| Segunda zona SphereCollider | `GuardProximity`, `GuardPerception.IsNear` | T44, Play Mode: proximidad dentro del radio, detrás de `RouteDivider`; el trigger de proximidad no atravesó la pared. |
| Reacción al detectar | `GuardSuspicion`, `GuardBrain` | T44, Play Mode: ambos acumularon sospecha de manera simultánea; se verificaron alerta, persecución, pérdida de visión e investigación. |
| Inicio, meta y obstáculos que permiten evitar detección | `M3_Decisions`, rutas, coberturas, carpeta y salida | T45, Play Mode: recorrido completo ganado por ambas rutas sin panel; hubo sospecha parcial, sin captura. |
| Victoria y derrota | `MissionFolder`, `ServiceExit`, `MissionManager`, `GuardBrain` | Play Mode: salida con carpeta produjo victoria; contacto expuesto durante Chase produjo derrota. Sin carpeta, la salida no gana. |

### Nota 7 — condiciones intermedias

| Requisito | Sistema | Prueba y resultado |
| --- | --- | --- |
| Patrulla, alerta y persecución | `GuardBrain` y HUD individual A/B | Captura `Assets/Screenshots/M5/M5-Indicadores.png`: A en alerta al 42%, B en persecución al 100%; nombre, estado, porcentaje y barra son independientes y legibles. |
| Feedback visual de estado y detección progresiva | `GuardSuspicion` + HUD por guardia | Play Mode: sospecha independiente simultánea 20%/40% sin cambiar patrulla; al perder exposición disminuyó. Barras muestran el valor antes de Chase. |
| Rutas múltiples y coberturas | `Route_Shelves_Marker`, `Route_Hall_Marker`, `ShelfCover_*`, geometría de M3 | T45, Play Mode: NavMesh completó ambos recorridos; una victoria por estanterías (A 11%, B 34%) y otra por pasillo (A 0%, B 89%). |
| Escondite que permite recuperarse | `HideoutInteractable`, `PlayerStealthState` | T44, Play Mode: A veía al jugador antes de entrar; dentro, visión y proximidad quedaron anuladas, A investigó y regresó a patrulla, y el jugador salió con controles restaurados. |
| Derrota al ser alcanzado | `GuardBrain` → `MissionManager.RegisterCapture` | T44, Play Mode: el contacto de un guardia en Chase produjo Defeat; esconderse impidió la captura. |
| Más de un enemigo simultáneo y diferente | `Guard_A`, `Guard_B`, circuitos y percepción serializados | T44, Play Mode: A vio al jugador mientras B lo percibió por proximidad; sospechas simultáneas 20% y 40%, estados y datos separados. |

### Nota 10 — condiciones avanzadas

| Requisito | Sistema | Prueba y resultado |
| --- | --- | --- |
| Código separado por responsabilidades | `Assets/Scripts/M1`–`M5`: percepción, navegación, sospecha, interacción, misión, UI y audio en componentes distintos | Inspección de escena y scripts: dos guardias con componentes independientes; cero scripts faltantes en `M3_Decisions`. |
| Detección progresiva antes de la detección completa | `GuardSuspicion` 0–100; umbrales de audio y HUD | Play Mode: visión/proximidad aumentaron valores parciales simultáneos; ambos continuaron en patrulla antes de llegar a Chase. |
| Enemigos, recorridos y rutas que generan situaciones variadas | Patrullas distintas A/B, parámetros visuales diferentes, coberturas y rutas de estanterías/pasillo | T45, Play Mode: ambas rutas dieron una victoria sin usar el panel; T44 verificó visión, proximidad y sospecha independientes. |
| Mecánica adicional de sigilo | `ServicePanel`, `BuzzerSignal`, investigación de A | T44, Play Mode: una activación durante Chase produjo el zumbador pero mantuvo Chase; una prueba anterior de M3 confirmó que A investiga el origen del sonido si no persigue. |
| Feedback visual, sonoro e interfaz | HUD A/B, objetivo, interacción contextual, inicio/pausa/resultados y `ProceduralAudioFeedback` | Play Mode: menú/controles, captura de HUD y resultado; el componente generó seis tonos en memoria, `AudioListener` activo y `AudioSource.isPlaying=True` tras el zumbador. No se importaron archivos de audio externos. La audición de todos los eventos en la build queda pendiente junto con T46. |

## Evidencia final de M5

- `Assets/Scenes/M3_Decisions.unity`: escena final; validación MCP de Unity: 0 problemas, 0 scripts faltantes, 0 prefabs rotos.
- `Assets/Screenshots/M5/M5-Menu.png`: título, controles y botón para iniciar, sin HUD de guardias sobre el menú.
- `Assets/Screenshots/M5/M5-Indicadores.png`: A y B visibles simultáneamente con estados y sospechas distintos.
- `Assets/Screenshots/M5/M5-Victoria-Recuperacion.png`: victoria tras cortar una persecución con el armario; los dos guardias volvieron a patrullar con sospecha 0.
- Build final Windows x64: `Builds/M5/ElUltimoTurno.exe` (junto a `ElUltimoTurno_Data`).
- Build MCP: finalizó para Windows64 (97.82 MB, 0 errores, 2 advertencias). Al iniciar el ejecutable, `Player.log` confirmó la inicialización del motor y registró dos avisos `Failed to create agent because there is no valid NavMesh`; se requiere una prueba interactiva adicional para determinar si afectan la patrulla. No se cuentan como éxito de comportamiento externo.
- Limitación de verificación externa: el proceso visible `ElUltimoTurno.exe` tuvo ventana `Sigilo`, pero la sesión automatizada devolvió `GetForegroundWindow = 0` y no permitió enviar controles ni capturar la pantalla; el otro proceso de prueba quedó sin ventana. Por eso no se afirma haber probado movimiento, cámara, interacciones, audio ni resultados dentro de la build. T46 permanece pendiente hasta repetir esa batería en una sesión de escritorio interactiva y resolver/descartar los avisos NavMesh.

