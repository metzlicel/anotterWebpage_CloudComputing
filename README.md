# Anotter Webpage
Proyecto final Computo en la Nube

Plataforma de manejo de agenda para la Dra. Connie Lopez. 
Crea citas y colaboraciones a través de la página web.

## Equipo: 
* Metzli Lopez 36639
* Marlon Corona 36505
* Priscila Nieto 33599
* Alan Lopez 33628

## Inicialización de la doctora

Después de aplicar las migraciones, cree la cuenta Identity y el perfil de la
doctora mediante el comando administrativo. No guarde su contraseña en el
repositorio:

```bash
dotnet user-secrets set "DoctorBootstrap:Email" "doctora@example.com" --project anotterwebpage_WebApi
dotnet user-secrets set "DoctorBootstrap:Password" "<contrasena-segura>" --project anotterwebpage_WebApi
dotnet run --project anotterwebpage_WebApi -- --bootstrap-doctor
```

El comando muestra el `UserId` creado. Guárdelo para los arranques posteriores
y elimine la contraseña de los secretos cuando termine:

```bash
dotnet user-secrets set "DoctorBootstrap:UserId" "<identity-user-id>" --project anotterwebpage_WebApi
dotnet user-secrets remove "DoctorBootstrap:Password" --project anotterwebpage_WebApi
```

El comando es idempotente: si la cuenta ya existe para el email configurado,
solo verifica el rol y el perfil. En despliegues, use las variables de entorno
equivalentes `DoctorBootstrap__UserId`, `DoctorBootstrap__Email` y, únicamente
para el alta inicial, `DoctorBootstrap__Password`.
