# Plan de tareas — El último turno

**Versión:** 0.2 — organizado por hitos jugables  
**Diseño de referencia:** [GDD_El_Ultimo_Turno.md](GDD_El_Ultimo_Turno.md)  
**Objetivo:** terminar el prototipo de la consigna, validando pronto su ciclo principal de sigilo.

## Instrucciones para la IA ejecutora

1. Leer el GDD completo antes de implementar. Ejecutar las tareas en orden **T01 → T47**. Las secciones agrupan el tipo de trabajo y cada hito termina con una prueba jugable.
2. Mantener la casilla [ ] hasta **cumplir y comprobar** el criterio de aceptación. Al completarla, cambiarla por [x] y agregar debajo una línea de evidencia con archivo o escena, prueba y resultado. Si una prueba no puede ejecutarse, dejar la casilla vacía y explicar el bloqueo.
3. Inspeccionar el proyecto y Git antes de editar. Reutilizar código y escena existentes sin sobrescribir cambios locales. Cuando se redactó este plan había modificaciones en escena, jugador, controles y paquetes.
4. Prioridades: **MUST** significa necesario para la consigna o para validar el ciclo jugable; **SHOULD** mejora la comprobación sin sustituir un MUST; **POLISH** se reserva para acabado opcional. El plan no incluye tareas POLISH porque ninguna es necesaria para esta prueba de concepto. Todos los MUST deben funcionar antes de dar la entrega por terminada.
5. Usar primero primitivas, materiales planos y UI básica. No importar modelos, texturas, animaciones, música ni audio externo. El primer ciclo jugable puede no tener sonido. La **entrega final sí debe tener feedback sonoro**, porque la consigna para nota 10 lo pide; puede resolverse con sonidos simples generados dentro del proyecto.
6. Los números del GDD son valores iniciales. Ajustarlos con pruebas. No añadir combate, moneda arrojadiza, inventario complejo, temporizador ni sistema de oscuridad.
7. Una prueba de hito se realiza en Play Mode; la build temprana y la final deben probarse fuera del editor. Registrar resultados observados, no solo que un script compila.

**Hitos:** M1, ciclo principal → M2, recuperación → M3, decisiones → M4, misión completa → M5, entrega.

---

## 0. Diagnóstico y preparación — tipo: proyecto

- [x] **T01 [MUST] Inventariar el proyecto.** Revisar escena, scripts, Input System, paquetes, versión de Unity y estado de Git. **Aceptación:** registrar qué se reutiliza, qué falta y cuáles son los cambios locales previos; ninguna modificación previa se pierde.
  - Evidencia: Unity 6000.3.11f1; `SampleScene` reutiliza `PlayerScr` (Rigidbody/cámara), `ControlsController` y `InputSystem_Actions` (WASD + mouse); AI Navigation 2.0.11 e Input System 1.19.0 instalados. No había guardias, NavMesh ni sandbox. Git estaba limpio al comenzar; se conservaron `SampleScene` y los scripts previos.
- [x] **T02 [MUST] Fijar escena de prueba y estructura mínima.** Preparar una escena de prueba para el primer ciclo jugable y carpetas para los nuevos scripts y prefabs. **Aceptación:** la escena abre en Unity sin referencias rotas y puede ejecutarse en Play Mode; los recursos nuevos tienen ubicación identificable.
  - Evidencia: `Assets/Scenes/M1_Sandbox.unity` abre en Unity; `manage_scene validate` informó 0 problemas, scripts faltantes o prefabs rotos. Scripts en `Assets/Scripts/M1/`, materiales en `Assets/Materials/M1/` y carpeta de prefabs en `Assets/Prefabs/M1/`.
- [x] **T03 [MUST] Registrar línea de base.** Ejecutar la escena existente antes de cambios funcionales. **Aceptación:** registrar errores de consola preexistentes y comprobar que las tareas de preparación no introdujeron errores nuevos.
  - Evidencia: `SampleScene` ejecutó en Play Mode y renderizó jugador/cámara (`Assets/Screenshots/screenshot-20260928-101208.png`). Antes de cambios: 0 errores y 2 advertencias preexistentes (`PlayerScr.cs:29` oculta `Component.camera`; `TriggerDetect.cs:9` campo no usado). Tras recompilar los scripts M1, la consola volvió a mostrar únicamente esas 2 advertencias; no hubo errores nuevos.

## 1. Hito M1 — Ciclo principal de sigilo — tipos: jugador, navegación y guardia

**Meta del hito:** en una escena mínima con suelo, pared, jugador, guardia y dos puntos de patrulla funciona el ciclo patrulla → percepción → sospecha → persecución → pérdida de visión → investigación → patrulla, con derrota por captura.

- [x] **T04 [MUST] Construir el sandbox mínimo.** Usar primitivas para suelo, una pared de cobertura y dos puntos de patrulla. **Aceptación:** jugador y guardia pueden caminar alrededor de la pared; esta bloquea el paso y el futuro rayo de visión.
  - Evidencia: `Assets/Scenes/M1_Sandbox.unity`, escena grisbox con suelo 20×20, CoverWall y ruta NavMesh alrededor de la cobertura; simulación en Play Mode recorrió ambos lados y el Rigidbody se detuvo en la cara de la pared (x=-2.48, cara x=-2). Un raycast desde el guardia contra el jugador al otro lado dio bloqueado.
- [ ] **T05 [MUST] Validar movimiento con físicas.** Adaptar el controlador existente y sus acciones de teclado. **Aceptación:** en Play Mode, WASD mueve al jugador por un circuito alrededor de la pared sin atravesar colliders, saltar de posición o quedar bloqueado en las cuatro esquinas.
  - Pendiente: se ejercitó PlayerScr/Rigidbody con movimiento simulado por ControlsController en Play Mode; el mapa de teclado no recibió la inyección de teclas del editor y no se pudo verificar WASD desde un teclado real. Posiciones recorridas sin atasco: (-4,1,1.2), (4,1,1.2), (4,1,-1.2), (-3.79,1,-0.87).
- [ ] **T06 [MUST] Validar cámara en tercera persona.** Ajustar seguimiento, mouse, límite vertical y colisión. **Aceptación:** completar el circuito de T05 mirando en distintas direcciones; cuando la pared queda entre cámara y jugador, la cámara se acerca y después recupera su distancia.
  - Pendiente: en Play Mode se alimentó MouseDelta al controlador y se verificaron yaw 30°, pitch limitado a -35°, acortamiento de cámara a 1.68 m ante pared y recuperación a 4.2 m. Falta repetir el circuito con mouse físico, ligado a la limitación de entrada real de T05.
- [x] **T07 [MUST] Preparar navegación del guardia.** Configurar el sistema de navegación del sandbox y el agente. **Aceptación:** el guardia alcanza ambos puntos desde cada lado de la pared sin atravesarla; repetir dos vueltas completas.
  - Evidencia: Play Mode, NavMesh horneado (88 vértices); agente en NavMesh completó dos transiciones de vuelta tras 37 s simulados y rodeó la pared por la ruta. Sin atravesar cobertura.
- [x] **T08 [MUST] Separar componentes del guardia.** Crear responsabilidades independientes para percepción, sospecha, comportamiento y navegación. **Aceptación:** es posible cambiar distancia visual o velocidad de una instancia sin editar lógica compartida ni alterar otra instancia.
  - Evidencia: `Assets/Scripts/M1/GuardBrain.cs`, `GuardPerception.cs`, `GuardSuspicion.cs`, `GuardNavigation.cs` y `GuardProximity.cs`. En Play Mode una instancia duplicada usó alcance 3 y velocidad 0.5 frente a 6 y 2 en la fuente; la fuente no cambió.
- [x] **T09 [MUST] Implementar patrulla.** Recorrer los dos puntos y detenerse brevemente en al menos uno. **Aceptación:** observar dos ciclos completos y comprobar que retoma el recorrido tras cada pausa.
  - Evidencia: Play Mode a escala normal durante 65 frames (1.30 s): agente pausado 51 frames y luego retomó ruta al waypoint 1; además completó dos vueltas simuladas.
- [x] **T10 [SHOULD] Crear depuración visual del guardia.** Mostrar en editor/Play Mode cono, radio cercano, rayo de visión, destino, última posición conocida, waypoint, estado y sospecha; mostrar ruta de navegación cuando haga falta. **Aceptación:** al seleccionar el guardia se pueden identificar esos valores durante una detección y una pérdida de visión; la visualización no aparece en la interfaz final.
  - Evidencia: al seleccionar Guard_A, `Assets/Screenshots/M1/M1-Debug-Gizmos.png` muestra cono y alcance, esfera de proximidad, rayo, waypoint/ruta y marcadores de estado/última posición. Estado y sospecha también se leen en HUD; gizmos de editor no aparecen en la captura Game View.
- [x] **T11 [MUST] Implementar visión con Raycast.** Considerar ángulo, distancia y obstáculos. **Aceptación:** desde tres posiciones de prueba, el jugador es visible frente al guardia sin obstáculos; no es visible detrás del guardia, fuera del alcance o detrás de la pared.
  - Evidencia: consulta en Play Mode: posición frontal visible; trasera, fuera de rango y con pared devolvieron no visible (rayo obstruido cuando correspondía).
- [x] **T12 [MUST] Implementar proximidad con SphereCollider.** Detectar cercanía fuera del cono visual, respetando obstáculos sólidos. **Aceptación:** acercarse por detrás sin pared activa percepción; repetir la misma distancia con pared entre ambos no la activa.
  - Evidencia: contacto de SphereCollider real simulado en Play Mode: detrás y sin obstáculo, proximidad activa; a igual posición con pared, el trigger bruto ocurre pero LOS lo filtra y no activa proximidad ni sospecha.
- [x] **T13 [MUST] Implementar sospecha independiente.** Mantener un valor 0–100 separado del estado; acumular con visión o cercanía y reducir sin contacto. **Aceptación:** la sospecha sube durante exposición continua, baja al cubrirse y el guardia puede seguir patrullando mientras el valor es parcial; la proximidad la aumenta más rápido que la visión.
  - Evidencia: en Play Mode, 0.8 s simulados produjo 40% con estado Patrol; al llenar la barra cambió a Chase. El evaluador dio tasa de proximidad 20 frente a 10 de visión; tras perder contacto durante investigación el valor descendió a 0.
- [x] **T14 [MUST] Implementar persecución.** Entrar en ella al llegar a 100 y seguir al jugador visible rodeando la pared. **Aceptación:** una exposición sostenida inicia la persecución; el guardia alcanza el otro lado de la pared siguiendo navegación y no atraviesa el obstáculo.
  - Evidencia: exposición completa en Play Mode activa Chase; destino acompaña al jugador visible y NavMesh circunda CoverWall sin cruzarla.
- [x] **T15 [MUST] Implementar pérdida de visión, investigación y regreso.** Guardar última posición vista sin seguir leyendo la posición actual del jugador oculto. **Aceptación:** tras perseguir, el jugador corta visión detrás de la pared; el guardia va a la última posición vista, busca un tiempo limitado y vuelve a los dos waypoints.
  - Evidencia: después de ocultar al jugador durante Play Mode, el estado cambió a Investigation, el destino conservó la última posición vista (-3.5,1,-1) y no siguió al jugador desplazado. Al agotarse búsqueda limitada volvió a Patrol, sospecha 0 y destino de waypoint.
- [x] **T16 [MUST] Implementar captura.** Derrota únicamente por contacto de un guardia en persecución con jugador expuesto. **Aceptación:** contacto en patrulla y sospecha parcial no derrota; contacto durante persecución sí; perder visión sin contacto permite sobrevivir.
  - Evidencia: en Play Mode, contacto a 0.8 m con Patrol y sospecha 60% dejó estado Patrol, sospecha 61% y escala de tiempo 1; al perder visión sin contacto, sobrevivió a investigación y regreso; contacto expuesto a 1 m en Chase produjo captura y derrota.
- [x] **T17 [MUST] Añadir feedback mínimo del ciclo.** Mostrar en Play Mode estado y sospecha con texto o color simple. **Aceptación:** observando la pantalla, una persona puede distinguir patrulla, sospecha parcial, investigación y persecución sin consultar el Inspector.
  - Evidencia: `Assets/Screenshots/M1/M1-Capture-final.png` muestra HUD legible con persecución, porcentaje y barra roja, más mensaje de derrota; durante la búsqueda el HUD expuso estado Investigation y sospecha. La barra parcial cambia a amarillo.
- [ ] **T18 [MUST] Validar M1 jugando.** Realizar al menos un intento de escapar y uno de dejarse capturar. **Aceptación:** documentar una secuencia completa del ciclo de la meta del hito y una derrota; ningún estado queda atascado ni aparecen errores nuevos de consola.
  - Pendiente: los estados y derrota se verificaron en Play Mode con colocación/movimiento simulados a través de Unity MCP; la secuencia integrada jugada con teclado/mouse reales queda pendiente por limitación de entrada interactiva (T05–T06). No se observaron estados atascados; consola sin errores y solo las dos advertencias de línea de base.

## 2. Hito M2 — Recuperación — tipo: escondite y estabilidad del guardia

**Meta del hito:** el jugador puede cortar una persecución, recuperar el control y seguir jugando.

- [ ] **T19 [MUST] Incorporar entrada contextual de interacción.** Añadir tecla de interacción y selección estable de un objeto cercano. **Aceptación:** una pulsación activa una sola vez un objeto de prueba dentro del alcance; fuera del alcance no hace nada; la selección no alterna sin control entre dos objetos cercanos.
  - Pendiente parcial: en Play Mode, la selección eligió el armario dentro de 1.8 m, quedó vacía fuera de alcance y no alternó en 20 refrescos con dos candidatos próximos; la ruta `Interact` del armario se ejercitó directamente. La sesión no permitió generar una pulsación física de E en la ventana de Unity, así que la aceptación completa de entrada permanece sin verificar.
- [x] **T20 [MUST] Crear el armario grisbox.** Permitir entrada y salida, inmovilizar al jugador dentro y adaptar cámara. **Aceptación:** entrar y salir diez veces consecutivas conserva posición y controles; dentro no se puede caminar ni interactuar con otros objetos.
  - Evidencia: `Assets/Scenes/M2_Recovery.unity` y `Assets/Screenshots/M2/M2-Hideout-View.png`. Diez ciclos de entrada/salida en Play Mode conservaron el estado; una comprobación adicional inyectó movimiento mientras estaba oculto y verificó posición estacionaria, bloqueo de movimiento y selección contextual vacía. La cámara permaneció anclada dentro; al salir se restauraron Rigidbody dinámico, collider y movimiento.
- [x] **T21 [MUST] Integrar ocultamiento con percepción y captura.** Excluir al jugador oculto de los dos sensores y del contacto de derrota. **Aceptación:** un guardia junto al armario no acumula sospecha ni captura al jugador dentro; al salir, vuelve a percibirlo.
  - Evidencia: Play Mode en `M2_Recovery`: con el guardia a menos de 0.2 m, proximidad y visión quedaron desactivadas al ocultarse; el guardia no capturó ni pausó el tiempo y al salir volvió a poder percibir al jugador expuesto.
- [x] **T22 [MUST] Resolver persecución al esconderse.** Los guardias pasan a investigar la última posición conocida y luego patrullan. **Aceptación:** entrar al armario durante persecución evita la derrota; tras la búsqueda, el guardia completa otra vuelta de patrulla.
  - Evidencia: Play Mode: en Chase, esconderse apagó ambos sensores y cambió el estado a Investigate sobre la última posición; al terminar la búsqueda volvió a Patrol con sospecha 0. El agente en NavMesh completó después el circuito de cuatro waypoints (índice 3→0), manteniéndose en Patrol.
- [x] **T23 [MUST] Validar M2 jugando.** Provocar persecución, esconderse, esperar el retorno, salir y volver a avanzar. **Aceptación:** ejecutar el ciclo tres veces sin persecución infinita, detección dentro del armario ni bloqueo al salir.
  - Evidencia: tres ciclos Play Mode con Chase → ocultarse → Investigate → Patrol, sensores apagados dentro y sospecha resuelta; en cada repetición se salió y el jugador se desplazó después. Un seguimiento adicional confirmó que el agente reanudó la patrulla desde la pausa de waypoint. Sin errores de proyecto observados.
- [ ] **T24 [MUST] Hacer una build temprana de PC.** Incluir el sandbox, cámara, guardia e interacción del armario. **Aceptación:** fuera del editor funcionan movimiento, mouse, cursor, navegación, interacción, persecución y escondite; registrar cualquier diferencia respecto de Play Mode.
  - Pendiente de aceptación completa: build Windows 64-bit Development generada en `Builds/M2/ElUltimoTurno-M2.exe` (161.16 MB; 0 errores, 4 advertencias de build). No se pudo manejar una ventana de aplicación nativa desde la sesión disponible (sin apps/nativas expuestas), por lo que movimiento, mouse, interacción y escondite fuera del editor quedan sin verificar; no se marca completa.

## 3. Hito M3 — Decisiones — tipos: nivel, segundo guardia y panel

**Meta del hito:** elegir entre rutas y decidir cuándo usar el panel cambia el modo de avanzar.

- [ ] **T25 [MUST] Construir el nivel grisbox con dos rutas.** Crear entrada, estanterías, pasillo, archivo y salida con conexiones que permitan cambiar de ruta. **Aceptación:** caminar desde entrada a archivo y salida por ambas rutas sin salir del área jugable ni atravesar sólidos.
- [ ] **T26 [MUST] Diferenciar las rutas con geometría.** Estanterías: recorrido más largo con cobertura. Pasillo: recorrido más directo y expuesto. **Aceptación:** al medir recorridos, el de estanterías requiere más distancia o tiempo de desplazamiento; en él hay al menos dos coberturas útiles; el pasillo tiene un tramo abierto que exige esperar al guardia B.
- [ ] **T27 [MUST] Reubicar al guardia A y navegación.** Configurar su circuito en estanterías y archivo; reconstruir navegación del nivel. **Aceptación:** A completa dos vueltas sin cruzar mobiliario y deja al menos una oportunidad de avanzar por cobertura sin alcanzar 100 de sospecha.
- [ ] **T28 [MUST] Añadir al guardia B.** Configurar un circuito de pasillo, pausas de mirada y visión diferente. **Aceptación:** A y B patrullan simultáneamente durante dos ciclos; sus rutas y zonas visuales son distintas y cada uno conserva sospecha/última posición independientes.
- [ ] **T29 [MUST] Crear panel y zumbador fijos.** Interacción desde el panel y evento en una posición distinta del archivo. **Aceptación:** activar el panel dentro del alcance señala el zumbador; hacerlo lejos no funciona y la activación no entrega al guardia la posición del jugador.
- [ ] **T30 [MUST] Hacer investigar el zumbador.** A responde si está en alcance y no persigue al jugador. **Aceptación:** A llega al zumbador, busca y vuelve a patrullar; si está persiguiendo, continúa la persecución; B no responde en todas sus posiciones de patrulla.
- [ ] **T31 [MUST] Añadir recarga visible al panel.** Evitar activaciones continuas y mostrar disponibilidad. **Aceptación:** dos pulsaciones durante la recarga producen un único evento; al expirar, una nueva pulsación produce otro.
- [ ] **T32 [MUST] Validar M3 jugando.** Probar ambas rutas con y sin panel, y escapar por el armario si hay detección. **Aceptación:** se documenta un recorrido viable por cada ruta; el panel permite una oportunidad distinta según el momento de activación; ninguna ruta exige usarlo.
- [ ] **T33 [SHOULD] Evaluar estrategias dominantes.** Observar si armario y panel eliminan la necesidad de mirar patrullas. **Aceptación:** registrar al menos tres intentos con decisiones distintas; si siempre gana la misma secuencia automática, ajustar primero posiciones o tiempos y repetir la prueba, sin añadir mecánicas nuevas.

## 4. Hito M4 — Misión completa — tipos: objetivo, estados de partida y UI mínima

**Meta del hito:** un intento puede empezar, ganar, perder y reiniciarse.

- [ ] **T34 [MUST] Implementar carpeta.** Crear un objeto único interactuable en el archivo. **Aceptación:** se recoge una sola vez, cambia de estado visual y actualiza el objetivo; una segunda interacción no duplica la recogida.
- [ ] **T35 [MUST] Implementar salida y victoria.** Comprobar posesión de carpeta en la zona de salida. **Aceptación:** entrar sin carpeta no gana y muestra qué falta; entrar con carpeta activa victoria una sola vez.
- [ ] **T36 [MUST] Implementar estado global y reinicio.** Gestionar jugando, pausa, victoria, derrota y restauración. **Aceptación:** después de reiniciar vuelven jugador, carpeta, ambos guardias, sospechas, armario y recarga del panel a condiciones iniciales; repetir tres partidas seguidas.
- [ ] **T37 [MUST] Mostrar objetivo e interacciones.** Usar UI simple para objetivo actual y acciones de carpeta, armario y panel. **Aceptación:** el objetivo cambia al recoger la carpeta; las indicaciones aparecen solo dentro del alcance y muestran la acción correcta.
- [ ] **T38 [MUST] Mostrar resultado y reintento.** Crear avisos funcionales de victoria/derrota y botón o tecla para reintentar. **Aceptación:** ganar y perder detienen la partida; reintentar desde ambos estados permite una nueva partida sin reiniciar Unity.
- [ ] **T39 [MUST] Validar M4 jugando.** Completar una partida sin ser detectado y otra tras esconderse; provocar además una derrota. **Aceptación:** las tres secuencias llegan al resultado correcto y se puede reintentar después de cada una.

## 5. Hito M5 — Entrega — tipos: claridad, feedback, pruebas y build

**Meta del hito:** el prototipo cumple la consigna completa y puede probarse fuera del editor.

- [ ] **T40 [MUST] Terminar indicadores de guardia.** Mostrar patrulla, “alerta” durante investigación, persecución y barra de sospecha por guardia. **Aceptación:** con A y B activos se distingue cuál detecta, cuánto le falta y su estado; información legible sin depender solo del color.
- [ ] **T41 [MUST] Incorporar feedback sonoro simple.** Producir dentro del proyecto sonidos breves para sospecha, persecución, zumbador, carpeta y resultados; pasos si ayudan a anticipar cercanía. **Aceptación:** cada evento se oye al ocurrir en la build y no se repite continuamente por error; ningún archivo de arte o audio externo es necesario.
- [ ] **T42 [MUST] Crear inicio y pausa funcionales.** Mostrar título, controles esenciales, jugar, pausar, reanudar y reiniciar. **Aceptación:** la partida inicia desde el menú; en pausa los guardias dejan de avanzar; el cursor se libera en menús y se captura al jugar.
- [ ] **T43 [MUST] Mejorar legibilidad grisbox.** Usar primitivas, materiales planos, luces y señalización básica para cobertura, guardias, armario, panel, carpeta y salida. **Aceptación:** una persona puede identificar esos seis tipos de elementos y distinguir las rutas durante una partida sin consultar la jerarquía de Unity.
- [ ] **T44 [MUST] Probar casos límite.** Cubrir visión detrás de pared, proximidad con pared, sospechas simultáneas, pérdida de vista, panel durante persecución, ocultamiento, contacto y reinicio. **Aceptación:** todos los casos coinciden con el GDD; no hay detección a través de sólidos, captura dentro del armario ni estado de guardia bloqueado.
- [ ] **T45 [MUST] Ajustar ritmo y dificultad.** Jugar varios intentos y ajustar velocidades, alcances, tiempos y patrullas. **Aceptación:** desde cada entrada de ruta existe al menos una ventana por ciclo para avanzar sin llegar a 100 de sospecha; se registra una partida ganada por cada ruta y una partida ganada tras escapar de persecución.
- [ ] **T46 [MUST] Generar y probar build final de PC.** Configurar escenas necesarias y ejecutar el juego fuera de Unity. **Aceptación:** en la build se puede iniciar, mover, mirar, interactuar, esconderse, usar panel, ganar, perder, pausar y reintentar; no hay errores nuevos de consola del proyecto en la verificación final.
- [ ] **T47 [MUST] Auditar consigna y documentar entrega.** Comparar el resultado con cada requisito para nota 4, 7 y 10; redactar controles e instrucciones de ejecución. **Aceptación:** existe una lista de correspondencia requisito → escena/sistema → prueba realizada, y una persona ajena al desarrollo puede abrir la build y completar un intento siguiendo la guía.

## Estado

- **Tareas completadas:** 19 / 47.
- **Último hito validado:** ninguno (M1 conserva T05, T06 y T18 pendientes; M2 conserva T19 y T24 pendientes).
- **Bloqueos conocidos:** teclado y mouse físicos no pudieron ejercitarse mediante la sesión automatizada disponible; T19 no pudo validar la pulsación real de E y T24 no pudo probar controles en la build fuera del editor.
- **Alcance de esta revisión:** M2 T19–T24; T20–T23 completadas con evidencia de Play Mode. No se avanzó a M3.
