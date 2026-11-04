# Plan de implementación: anotterWebpage_CloudComputing

## Alcance acordado

Este plan cubre la API, su esquema de datos y sus pruebas.

- `Patient` y `Doctor` serán entidades persistidas y roles de Identity.
- Cada paciente será el titular de su cuenta. Habrá una doctora en esta versión.
- La doctora podrá consultar y gestionar todas las citas; cada paciente solo podrá consultar y gestionar las suyas.
- Cancelar conservará la cita con estado `Cancelada` y liberará su horario.
- Una modificación o cancelación efectiva notificará por correo al paciente y a la doctora.
- La creación de citas usará un método estático `Appointment.Create(...)` en la entidad de dominio.
- Las fechas futuras de las pruebas partirán de la hora actual más tres días. Se usará UTC por el tipo `timestamp with time zone` de PostgreSQL.
- Actualmente no hay citas guardadas. La migración puede adoptar el esquema nuevo directamente y todas las citas futuras deberán usarlo.

**Raíz del repositorio:** `/Users/marloncoronaarango/Developer/anotterWebpage_CloudComputing`. Las rutas de abajo son relativas a esa raíz. El proyecto original solo guarda los datos del paciente dentro de `Appointment` y envía los avisos a una dirección fija; `Patient`, `Doctor`, modificación y cancelación son ampliaciones del modelo original.

## 1. Fijar el contrato de datos de las citas nuevas

**Crear:** `anotterwebpage_WebApi/Domain/Patient.cs`, `Doctor.cs` y `AppointmentStatus.cs`.

**Modificar:** `anotterwebpage_WebApi/Domain/Appointment.cs` y `Api/Dtos/AppointmentDto.cs`.

1. Dar a `Patient` y `Doctor` una clave propia y un `UserId` único que apunte a `AspNetUsers`. Guardar en sus perfiles los datos profesionales o de contacto que necesite la API. Identity conserva los datos de acceso.
2. Agregar `PatientId`, `DoctorId`, `Status` y `CancelledAt` anulable a `Appointment`. Los estados iniciales serán `Confirmada` y `Cancelada`.
3. Hacer que los DTO de creación y modificación reciban los datos de la cita, no un `PatientId` libre. El servidor identifica al paciente con la cuenta autenticada. Mientras haya una sola doctora, el servidor asigna la doctora activa.
4. Retirar de `Appointment` los datos duplicados de persona y cuenta: `Nombre`, `Apellido`, `Email`, `Numero`, `NombrePaciente` y `UserId`. `Patient` conserva nombre y teléfono; Identity conserva el email de acceso; `Doctor` conserva su email profesional. Retirar también `IsReserved`, sustituido por `Status`. `Motivo`, `Modalidad`, fechas y `CreatedAt` permanecen en la cita.
5. Ajustar los DTO y el mapeo para mostrar los datos de Patient y Doctor cuando corresponda, junto con el estado. El DTO de disponibilidad solo expone ID, inicio, fin e `isMine`, calculado con el usuario autenticado.

**Salida verificable:** ninguna cita nueva puede existir sin paciente, doctora y estado; el destinatario del correo se obtiene de las entidades vinculadas.

## 2. Actualizar el esquema de PostgreSQL para las citas nuevas

**Modificar:** `anotterwebpage_WebApi/Api/Data/ApplicationDbContext.cs` y `Api/Extensions/MappingExtensions.cs`.

1. Agregar `DbSet<Patient>` y `DbSet<Doctor>`; configurar relaciones uno a uno con Identity e índices únicos para `UserId`. Configurar las relaciones de `Appointment` con ambas entidades y el estado.
2. Generar una migración nueva con EF Core. No modificar `InitialCreate` ni `AddIdentityUsers`.
3. Verificar que `Appointments` esté vacía antes de aplicar la migración. Añadir `PatientId`, `DoctorId` y `Status` como columnas obligatorias, `CancelledAt` como anulable, y retirar las columnas duplicadas indicadas en el paso 1. No hace falta un proceso de traslado de citas ni columnas temporales de compatibilidad.
4. Crear o vincular el perfil de la doctora antes de aceptar la primera reserva. La creación de perfiles Patient para cuentas Identity que ya existan, si las hay, se resuelve en el flujo de cuentas del paso 3; no depende de citas previas.

**Salida verificable:** la migración se aplica sobre `Appointments` vacía y el esquema exige los vínculos y el estado de todas las citas que se creen después.

## 3. Registrar roles y crear perfiles de forma controlada

**Modificar:** `anotterwebpage_WebApi/Api/Program.cs`. **Crear:** código de inicialización de roles y de alta de perfiles en `Api` o `Services`.

1. Agregar servicios de roles a Identity y crear idempotentemente los roles `Patient` y `Doctor`.
2. Establecer un único flujo de registro de pacientes que cree la cuenta Identity, el perfil Patient y la asignación del rol. Revisar el `/api/auth/register` que aporta `MapIdentityApi`: no debe quedar una segunda vía pública que cree usuarios sin perfil ni rol. Si ya hay cuentas Identity, crear o vincular sus perfiles antes de darles acceso a las operaciones de Patient.
3. Dar de alta a la doctora mediante una operación controlada de inicialización o administración. No permitir que el registro público asigne el rol Doctor ni guardar una contraseña inicial en el código.
4. Resolver la doctora activa desde el repositorio. Sustituir la dirección administrativa fija en los correos de citas por el email configurado en su perfil.

**Salida verificable:** un registro público produce exactamente una cuenta Patient con su perfil; repetir la inicialización no duplica roles ni a la doctora.

## 4. Añadir los repositorios Patient y Doctor

**Crear:** `Repositories/IPatientRepository.cs`, `PatientRepository.cs`, `IDoctorRepository.cs` y `DoctorRepository.cs`.

1. Añadir búsquedas por `UserId` y por ID; para Doctor, una consulta de la doctora activa. Registrar interfaces e implementaciones en `Program.cs`.
2. Extender `IAppointmentRepository` y `AppointmentRepository` para consultar citas por paciente, consultar todas para la doctora y excluir las canceladas de `GetBusyAsync` y `HasOverlapAsync`.
3. Mantener EF Core dentro de los repositorios. Los delegates coordinan reglas de negocio, autorización por propiedad y persistencia.

**Salida verificable:** una cita cancelada sigue disponible para consulta histórica y ya no bloquea el horario.

## 5. Crear y cambiar citas en el dominio

**Modificar:** `Domain/Appointment.cs`, `Delegates/AppointmentDelegate.cs`, `Delegates/IAppointmentDelegate.cs` y los validadores de citas.

1. Añadir `Appointment.Create(...)` como factory estática. Recibe identificadores válidos de Patient y Doctor, fechas UTC y datos de consulta; comprueba invariantes del objeto y asigna el estado inicial. El dominio no recibe DTO ni depende de `Api`.
2. En `BookAsync`, resolver Patient desde el usuario autenticado y Doctor desde su repositorio, comprobar solapamiento, llamar a `Appointment.Create(...)` y guardar mediante `IAppointmentRepository`.
3. En `UpdateAsync`, rechazar citas canceladas, comprobar propiedad o rol Doctor, guardar una copia de los valores anteriores, validar el nuevo horario, modificar la entidad y persistirla. Enviar aviso solo si hubo un cambio efectivo.
4. Cambiar `DeleteAsync` por una operación de cancelación que marque `Cancelada` y `CancelledAt`. Un segundo intento de cancelación no debe generar otro correo. Definir y probar una respuesta HTTP estable para ese caso.

**Salida verificable:** la factory crea una cita válida; modificación y cancelación respetan el estado, el propietario y los solapamientos.

## 6. Aplicar autorización en las rutas

**Modificar:** `Api/Routes/AppointmentRoutes.cs` y, si se exponen perfiles, crear rutas Patient y Doctor con DTO propios.

| Operación | Acceso previsto |
| --- | --- |
| Crear cita | Patient autenticado, para sí mismo |
| Ver horarios ocupados | Usuario autenticado; sin datos personales |
| Ver cita por ID | Su Patient o Doctor |
| Listar todas las citas | Doctor |
| Modificar o cancelar | Su Patient o Doctor |

Aplicar autorización en el endpoint y volver a comprobar propiedad en el delegate. Rechazar explícitamente `userId` nulo: hoy la API admite reservas anónimas y compara identificadores nulos al revisar la propiedad. Si el cliente requiere ver solo sus citas, agregar una consulta `mine` en lugar de abrir el listado general.

**Salida verificable:** un paciente no puede leer ni modificar citas ajenas; la doctora sí puede gestionar todas; las respuestas de disponibilidad nunca incluyen nombre, email o teléfono.

## 7. Notificar creación, modificación y cancelación

**Modificar:** `Delegates/AppointmentDelegate.cs` y `Api/Data/ApplicationDbContext.cs`. **Crear:** un servicio de notificaciones de citas, plantillas, entidad y repositorio de notificaciones pendientes, y un servicio de fondo que las envíe mediante `IEmailService`.

1. Mantener el aviso de creación y añadir `Modificada` y `Cancelada`. En la modificación incluir el horario anterior y el nuevo, junto con la modalidad si cambió.
2. Preparar un mensaje para Patient y otro para Doctor. Tomar los destinatarios de sus perfiles, no del cuerpo editable de la solicitud ni de una dirección fija.
3. Para cumplir el envío aun si SMTP falla temporalmente, guardar una notificación pendiente en una tabla de salida dentro de la misma transacción que cambia la cita. Un proceso de fondo la envía, registra intentos y reintenta. Evitar duplicados mediante un identificador único por evento y destinatario. `EmailService` sigue encargándose del transporte SMTP.
4. Registrar un fallo de entrega para revisión. La API no debe informar que la cita no cambió cuando el cambio ya quedó guardado y solo falló el correo.

**Salida verificable:** cada cambio efectivo y cada primera cancelación generan dos notificaciones pendientes; un fallo temporal de SMTP no las pierde ni duplica el cambio de la cita.

## 8. Actualizar pruebas y fechas

**Modificar:** pruebas de `Domain`, `Repositories`, `Delegates` y `Api`, especialmente `AppointmentTests.cs`, `AppointmentRepositoryTests.cs`, `AppointmentDelegateTests.cs`, `Builders/AppointmentBuilder.cs`, `Routes/AppointmentRoutesTests.cs`, `Concurrency/ConcurrencyTests.cs` y la fábrica `AnotterwebpageApiTests.cs`.

1. Para citas futuras, capturar una vez por prueba `var start = DateTime.UtcNow.AddDays(3);` y derivar `end = start.AddHours(1)`. Construir solapamientos y huecos con desplazamientos de ese mismo `start`. No llamar varias veces a `UtcNow` dentro del mismo escenario. Las fechas históricas usadas para comprobar `CreatedAt` pueden seguir siendo fijas si la prueba no depende de que sean futuras.
2. Agregar pruebas de la factory, relaciones Patient/Doctor, inicialización idempotente de roles, registro de Patient, permisos por rol y propiedad, disponibilidad tras cancelar y rechazo de modificación de cita cancelada.
3. Verificar dos avisos para modificar y cancelar; cero avisos para una operación rechazada, un PUT sin cambios o una segunda cancelación. Probar los reintentos de correo con un servicio falso que falle y luego se recupere.
4. Adaptar las pruebas de rutas que hoy llaman sin autenticación y esperan `200`, `201` o `204`. Usar identidades Patient y Doctor en la fábrica de pruebas; comprobar `401` o `403` donde corresponda.
5. Ejecutar `dotnet test anotterwebpage_WebApi.sln`. Después, probar la migración con PostgreSQL y una base de ensayo cuya tabla `Appointments` esté vacía; crear una cita nueva y comprobar sus relaciones, estado y disponibilidad.

**Salida verificable:** todas las pruebas pasan sin depender del día de ejecución y las nuevas pruebas cubren los estados, permisos y notificaciones.

## Orden de entrega

1. Modelo y migración directa para una base sin citas.
2. Roles, perfiles y repositorios.
3. Factory y reglas de citas.
4. Autorización de rutas.
5. Cancelación conservada y correos con reintentos.
6. Pruebas, migración de ensayo y revisión del contrato HTTP.

Antes de cerrar la implementación, revisar también las rutas de colaboraciones: actualmente sus listados y operaciones de edición están abiertos. Su política de acceso no se deduce de los permisos acordados para Doctor y Patient, así que debe definirse por separado.
