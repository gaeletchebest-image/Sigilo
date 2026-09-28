# GDD — El último turno

**Versión:** 0.2 — incorpora la revisión de producción  
**Género:** sigilo en tercera persona  
**Motor:** Unity 6  
**Plataforma objetivo:** PC, teclado y mouse  
**Duración estimada de una partida:** 3 a 6 minutos  
**Alcance:** un nivel corto, dos guardias y una misión completa

## 1. Concepto

Durante el último turno de la noche, el jugador entra en un archivo de seguridad para recuperar una carpeta y escapar por la salida de servicio. Debe estudiar las patrullas, aprovechar la cobertura, usar un escondite si lo descubren y decidir si activa un panel que distrae a un guardia.

La experiencia debe ser fácil de entender en la primera partida y permitir varios intentos breves. El desafío consiste en elegir el momento y la ruta para avanzar, no en combatir.

### Pilares de diseño

1. **Información legible:** el jugador puede reconocer dónde miran los guardias y cuánto falta para ser descubierto.
2. **Decisiones de ruta:** siempre existe al menos una alternativa al pasillo más expuesto.
3. **Errores recuperables:** ser visto inicia una persecución; la derrota ocurre cuando un guardia alcanza al jugador.
4. **Alcance acotado:** cada sistema tiene una función clara dentro de un único nivel compacto.

## 2. Objetivo y reglas de la partida

**Objetivo principal:** recoger la carpeta del archivo central y llegar a la salida de servicio.

**Victoria:** el jugador entra en la zona de salida con la carpeta. La salida comunica de forma visible si todavía falta recogerla.

**Derrota:** cualquier guardia alcanza y toca al jugador durante una persecución. Se muestra una pantalla breve de derrota con opción de reintentar desde el inicio.

**Bucle principal:** observar una patrulla → elegir ruta y cobertura → avanzar → reaccionar ante la sospecha → recuperar la carpeta → escapar.

No hay combate, vidas, inventario complejo ni límite de tiempo. El jugador puede reintentar cuantas veces quiera. La carpeta es un único objeto de misión y no se pierde después de recogerla, salvo al reiniciar la partida.

## 3. Cámara y movimiento del jugador

- **Perspectiva:** tercera persona, con cámara detrás del personaje y control con mouse. La cámara debe acercarse si una pared se interpone para evitar que tape la vista.
- **Movimiento:** desplazamiento con teclado relativo a la orientación del personaje, implementado con `Rigidbody` y el sistema de físicas de Unity.
- **Velocidad:** una sola velocidad de movimiento para el prototipo. Se ajustará junto con la velocidad de los guardias para que la persecución sea peligrosa, pero dé tiempo a alcanzar un escondite cercano.
- **Colisiones:** paredes, estanterías y mobiliario bloquean el movimiento; el jugador no puede atravesar la cobertura.
- **Interacción contextual:** una tecla de interacción sirve para recoger la carpeta, entrar o salir de un escondite y activar el panel de servicio. Solo aparece una indicación cuando el objeto está al alcance.
- **Restricción al esconderse:** dentro del escondite el jugador no se mueve ni interactúa con otros objetos. Puede salir cuando quiera.

**Controles propuestos:** WASD para moverse, mouse para mirar, `E` para interactuar y `Esc` para pausa. Los controles definitivos se validarán con el sistema de entrada que ya tiene el proyecto.

## 4. Guardias

Hay **dos guardias simultáneos**, con los mismos estados generales pero recorridos y parámetros de percepción diferentes.

| Guardia | Ubicación principal | Comportamiento distintivo |
| --- | --- | --- |
| A: archivista de ronda | Estanterías y archivo central | Patrulla en circuito de varios puntos. Visión de alcance medio y giros frecuentes; hace peligrosa la ruta con cobertura. |
| B: vigilante del pasillo | Pasillo principal y salida | Patrulla entre pocos puntos y se detiene a mirar hacia la ruta directa. Ve más lejos en línea recta, pero deja ventanas para cruzar. |

Cada guardia tiene sus propios puntos de patrulla, nivel de sospecha y última posición conocida del jugador. Ambos pueden detectar y perseguir al mismo tiempo. No comparten información automáticamente.

### 4.1 Comportamiento y sospecha

El **estado de comportamiento** y el **nivel de sospecha** son variables separadas. Un guardia puede continuar su patrulla mientras aumenta su sospecha. La sospecha parcial, por sí sola, no lo obliga a caminar hacia el jugador.

| Estado | Entrada | Comportamiento | Salida |
| --- | --- | --- | --- |
| **Patrulla** | Inicio o fin de una investigación | Recorre sus puntos, se detiene brevemente en algunos y observa. Puede acumular sospecha mientras sigue su recorrido. | Pasa a investigación al perder un contacto parcial o percibir el panel; a persecución si la sospecha llega a 100. |
| **Investigación** | Pérdida de visión con sospecha acumulada, pérdida del jugador perseguido o sonido del panel | Va a la última posición vista o al origen del sonido y observa durante un tiempo limitado. | Vuelve a patrulla si no encuentra al jugador; pasa a persecución si completa la detección. |
| **Persecución** | Sospecha completa | Sigue la posición visible del jugador; si lo pierde, va a la última posición conocida. | Derrota si lo alcanza; pasa a investigación si pierde contacto o el jugador entra en un escondite. |

**Prioridad:** ver claramente al jugador tiene prioridad sobre investigar el panel. Una distracción no cancela una persecución activa.

**Retorno:** al terminar una investigación, el guardia se dirige al punto de patrulla más apropiado y retoma su circuito. El regreso debe ser visible mediante su indicador de estado. La interfaz puede mostrar “Alerta” al jugador durante la investigación, pero ese texto no representa un cuarto estado de comportamiento.

### 4.2 Navegación

Los guardias deben rodear paredes y mobiliario mediante navegación del nivel. Los puntos de patrulla se colocan manualmente. Se evita que los recorridos atraviesen escondites, metas u objetos sólidos. Si un objetivo queda inaccesible, el guardia debe abandonar esa búsqueda y reanudar la patrulla, sin quedar bloqueado indefinidamente.

## 5. Detección y sospecha

La detección tiene **dos zonas complementarias**:

1. **Visión a distancia:** el guardia comprueba que el jugador esté dentro de su alcance y ángulo frontal. Un `Raycast` desde la altura de sus ojos confirma que no haya una pared, estantería u otro obstáculo opaco entre ambos. La cámara del jugador no afecta esta prueba.
2. **Proximidad:** un `SphereCollider` de tipo trigger alrededor del guardia detecta al jugador cercano, incluso si está fuera del ángulo frontal. Los obstáculos sólidos siguen bloqueando la detección para evitar que un guardia perciba al jugador a través de una pared. El escondite anula ambas zonas mientras el jugador esté dentro.

El guardia no detecta al jugador por un único fotograma de visión. Cada uno acumula una **sospecha independiente de 0 a 100**:

- Con visión clara, la sospecha sube de forma continua.
- En proximidad, sube más rápido porque el jugador está muy cerca.
- Sin contacto sensorial, baja gradualmente. Si el guardia había percibido al jugador, investiga su última posición antes de volver a patrullar.
- Al llegar a 100, el guardia entra en persecución.
- Durante la persecución, la barra permanece llena; perder de vista al jugador inicia una búsqueda corta en la última posición conocida.

Si el guardia pierde de vista al jugador con sospecha parcial, pasa a investigación. Un sonido del panel inicia investigación sin sumar automáticamente sospecha del jugador. La sospecha y el estado de cada guardia son independientes de los del otro.

**Valores iniciales para probar, no definitivos:** visión del guardia A de 9 m y 100°; visión del B de 12 m y 75°; proximidad de 2 m; detección visual completa tras aproximadamente 2 segundos de exposición continua; detección cercana tras aproximadamente 1 segundo; búsqueda de 4 segundos. Se ajustarán en el nivel jugable para evitar detecciones injustas o esperas excesivas.

### Casos de claridad y justicia

- Una estantería que sirve de cobertura debe bloquear tanto el `Raycast` como la vista del jugador de manera reconocible.
- El indicador de sospecha debe bajar cuando se corta la línea de visión.
- El guardia puede investigar la última ubicación del jugador, pero no conocer su posición actual a través de obstáculos.
- Entrar en la zona de proximidad no causa derrota inmediata; primero genera sospecha, salvo que el guardia ya esté persiguiendo y alcance físicamente al jugador.
- Los guardias no ven al jugador dentro de un escondite.

## 6. Escondites

El nivel incluye **al menos un armario de mantenimiento** ubicado cerca de un cruce de rutas. Se reconoce por su forma, color y señal de interacción.

Al acercarse y pulsar interactuar, el jugador entra en el armario. Su personaje queda oculto, la cámara pasa a una vista fija o limitada desde el escondite y aparece el texto “E: salir”. El jugador no puede ser detectado ni alcanzado mientras permanezca dentro.

Si entra durante una persecución, los guardias que lo perseguían dejan de seguirlo. Pueden revisar brevemente la última posición conocida y luego vuelven a patrullar. Esto debe permitir recuperar el control de la situación sin convertir el escondite en una teletransportación. Al salir, el jugador vuelve a estar expuesto y los guardias pueden detectarlo de nuevo.

El escondite no es obligatorio para ganar: existe una ruta que permite evitar la detección usando tiempos y cobertura.

**Hipótesis de prueba:** el armario debe permitir recuperarse de un error sin convertirse en la única estrategia útil. Antes de imponer límites de uso, llaves o tiempos de espera, se probará si los jugadores repiten sistemáticamente “ser detectado → correr al armario → esperar” y si eso elimina la tensión.

## 7. Mecánica adicional: panel de servicio

La moneda queda fuera del diseño. En su lugar hay **un panel fijo e interactuable** al comienzo del sector central. Al activarlo, hace sonar un zumbador de mantenimiento en un punto visible del archivo.

- El guardia A, si no está persiguiendo al jugador y está dentro del alcance del sonido, pasa a investigación y va a revisar el zumbador.
- El sonido no informa al guardia quién activó el panel ni revela la posición del jugador.
- El panel tiene un tiempo de recarga visible para que no se pueda usar continuamente.
- El jugador puede completar el nivel sin activar el panel.

Esta interacción da una forma deliberada de alterar una patrulla. No requiere apuntar, lanzar objetos ni gestionar munición.

**Valor inicial para probar:** recarga de 20 segundos, duración de investigación de 4 segundos. El alcance del sonido debe cubrir la patrulla del guardia A, pero no atraer siempre al guardia B.

**Hipótesis de prueba:** importa cuándo se activa el panel. Se comprobará si usarlo durante distintas fases de la patrulla abre oportunidades diferentes. Si siempre basta con pulsarlo en cuanto está disponible, habrá que ajustar la ubicación del zumbador, las ventanas de patrulla o la recarga antes de agregar otra mecánica.

## 8. Diseño del nivel

### Distribución propuesta

```text
                  [ARCHIVO CENTRAL]
                carpeta + guardia A
                 /             \
 [ENTRADA] -- [ESTANTERÍAS] -- [PASILLO VIGILADO] -- [SALIDA]
               cobertura         guardia B
               armario
               panel de servicio
```

El diagrama muestra conexiones de juego, no la geometría final. Las dos rutas se cruzan antes del archivo y vuelven a conectarse hacia la salida. El jugador puede cambiar de ruta al ver que una patrulla bloquea su avance.

La **ruta de estanterías** es más lenta y ofrece cobertura y oportunidades de reposicionamiento; depende de observar al guardia A. El **pasillo vigilado** es más directo y abierto; depende de elegir una ventana en la patrulla del guardia B. Ambas deben poder completarse y ninguna debe ser claramente superior en todas las situaciones.

### Secuencia buscada

1. **Entrada:** zona segura breve para aprender a mirar, moverse y reconocer el objetivo.
2. **Primer cruce:** se ve al menos un guardia antes de quedar expuesto. El jugador elige el pasillo rápido o la ruta entre estanterías.
3. **Sector central:** guardia A, armario y panel introducen observación, cobertura, escondite y distracción.
4. **Archivo:** la carpeta se ve claramente desde la entrada del sector, pero recogerla requiere aprovechar una ventana de patrulla.
5. **Escape:** el jugador regresa por cualquiera de las rutas y cruza la salida mientras el guardia B sigue activo.

### Reglas de construcción

- Las rutas deben tener cobertura real, no solo decoración.
- Ningún guardia debe aparecer de forma sorpresiva justo detrás del jugador al iniciar o al recoger la carpeta.
- Los pasillos deben permitir que cámara, jugador y guardias se muevan sin atascarse.
- La iluminación debe ayudar a leer personajes, obstáculos, armario, panel, carpeta y salida. No se implementa un sistema de invisibilidad basado en luz.
- El nivel puede construirse con geometría simple y materiales de colores coherentes; el pulido se concentra en legibilidad y respuesta visual/sonora.

## 9. Interfaz y feedback

| Elemento | Información que comunica |
| --- | --- |
| Texto de objetivo | “Recuperá la carpeta” y luego “Llegá a la salida”. |
| Indicador sobre cada guardia | Patrulla, alerta durante la investigación o persecución mediante color/icono reconocible. |
| Barra de sospecha | Se muestra cuando un guardia empieza a detectar al jugador; corresponde a ese guardia. |
| Señal de interacción | Indica la tecla y la acción disponible para carpeta, armario o panel. |
| Estado del panel | Disponible o tiempo restante de recarga. |
| Aviso de salida | Comunica si falta la carpeta. |
| Pantallas de resultado | Victoria o derrota, con opción de reintento. |

**Colores propuestos:** verde o neutro para patrulla, amarillo para alerta, rojo para persecución. Los colores se acompañan con texto o iconos para que el estado no dependa solo del color.

**Sonido mínimo para la entrega:** pasos de guardia perceptibles a corta distancia, aviso corto al subir la sospecha, señal clara al comenzar la persecución, zumbador del panel, confirmación al recoger la carpeta y sonidos breves de victoria/derrota. Se prioriza que los sonidos informen decisiones del jugador. La primera versión jugable puede comunicar todo con texto y colores. No requiere importar arte ni audio externo; los sonidos finales pueden generarse de forma simple dentro del proyecto. Esto conserva el requisito de feedback sonoro de la consigna para nota 10.

**Arte del prototipo:** geometría básica de Unity, materiales de colores planos y UI sencilla. No se requieren modelos, texturas ni animaciones externas. La presentación final debe ser clara y coherente aunque siga siendo un grisbox.

## 10. Flujo de interfaz

1. **Inicio:** título, controles básicos y botón “Jugar”.
2. **Partida:** objetivo visible, indicadores contextuales y posibilidad de pausar.
3. **Pausa:** reanudar, reiniciar o volver al inicio.
4. **Victoria:** mensaje de misión completada y opción de volver a jugar.
5. **Derrota:** mensaje de captura y opción de reintentar inmediatamente.

Al reiniciar se restauran la carpeta, los guardias, sus sospechas, el panel y la posición del jugador.

## 11. Organización técnica prevista

Esta sección define responsabilidades para mantener el código separado; **no es todavía una lista de tareas de implementación**.

| Sistema | Responsabilidad principal |
| --- | --- |
| Control del jugador | Entrada, movimiento físico, orientación y bloqueo del movimiento al esconderse. |
| Interacción | Detectar objetos utilizables cercanos y ejecutar la interacción contextual. |
| Percepción del guardia | Visión con `Raycast`, proximidad con `SphereCollider` y cálculo de sospecha. |
| Comportamiento del guardia | Estados separados del valor de sospecha; patrulla, investigación, persecución y retorno. |
| Escondite | Entrada, salida y estado oculto del jugador. |
| Panel de servicio | Activación, recarga y emisión del evento de sonido. |
| Estado de partida | Carpeta, victoria, derrota, reinicio y pausa. |
| Interfaz y audio | Mostrar estado y reproducir señales según eventos del juego. |

El proyecto actual ya contiene una escena, movimiento físico en tercera persona y controles de movimiento/cámara. Esos elementos se revisarán y adaptarán cuando pasemos a implementar; este documento no supone que los demás sistemas ya existan.

## 12. Criterios para considerar completo el prototipo

- Se puede iniciar, jugar, ganar, perder y reiniciar sin usar el editor.
- El jugador se mueve mediante físicas y la cámara permite leer el entorno.
- Hay dos guardias activos con patrullas o áreas de vigilancia distintas.
- Cada guardia usa visión con distancia, obstáculos y `Raycast`, además de una zona cercana con `SphereCollider`.
- La sospecha progresa antes de la persecución y los estados de comportamiento patrulla, investigación y persecución son visibles; la interfaz puede llamar “alerta” a la investigación.
- La detección inicia persecución; solo el contacto de un guardia que persigue causa derrota.
- El armario permite cortar una persecución y los guardias vuelven a patrullar.
- El nivel tiene inicio, carpeta, salida, obstáculos, cobertura y al menos dos rutas viables.
- El panel de servicio altera una patrulla de manera comprensible.
- La interfaz y el audio comunican objetivo, interacción, detección y resultados.

## 13. Decisiones abiertas para revisar juntos

1. Nombre definitivo del juego y aspecto visual del archivo.
2. Ubicación exacta del armario y del panel dentro del nivel.
3. Velocidades, alcances y tiempos finales después de probar una versión jugable.
4. Si la carpeta debe quedar en el centro del archivo o en una pequeña sala lateral.
5. Si los ensayos muestran que el armario o el panel ofrecen decisiones interesantes con sus reglas iniciales.

Estas decisiones no cambian el alcance principal: una misión breve de sigilo con dos rutas, dos guardias, detección progresiva, escondite y una interacción fija de distracción.
