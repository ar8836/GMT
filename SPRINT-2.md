# Foco Actual del Sprint 2

| Clave | Resumen | Estado |
|-------|---------|--------|
5	| GMT-31 | Ajuste de políticas de acceso y CORS en S3 | **Completado** |
6	| GMT-30 | Depuración de tiempos de respuesta y entrega del servicio SMTP | **Completado** |
7	| GMT-29 | Refactorización y pruebas de estrés del motor de validación Regex | **Completado** |
8	| GMT-26 | Crear la interfaz de usuario básica para que las empresas puedan publicar los detalles de la vacante (Carrera dirigida, apoyo económico, perfil buscado). | **Completado** |
9	| GMT-25 | Implementar lógica de filtrado en el controlador de vacantes que.compare el campo porcentaje_creditos del alumno contra el requisito de la vacante (ej. 70%) | **En progreso** |
10	| GMT-24 | Como alumno de últimos semestres, quiero ver únicamente las vacantes de residencias que coincidan con mi carrera y para las cuales cumplo el requisito de créditos aprobados | **Tareas por hacer** |
11	| GMT-23 | Crear una vista básica de "Pendientes de Aprobación" para que el administrador del sistema pueda activar o rechazar cuentas de empresas | **Completado** |
12	| GMT-22 | Implementar el formulario de carga de documentos de identidad empresarial con destino al bucket de S3. | **Completado** |
13	| GMT-21 | Diseñar y ejecutar la migración en PostgreSQL para las tablas Empresas y Vacantes | **Completado** |
15	| GMT-20 | Como representante de una empresa, quiero registrar mi organización y adjuntar mi Constancia de Situación Fiscal (RFC) para que el administrador valide la existencia legal de mi negocio antes de publicar vacantes | **Tareas por hacer** |
15	| GMT-19 | Modificar el perfil de usuario para almacenar únicamente la URL del objeto de S3 en la base de datos de RDS, evitando guardar binarios en la DB | Tareas por hacer |
16	| GMT-18 | Integrar el AWS SDK en el proyecto ASP.NET MVC para gestionar la subida y lectura de archivos | **Completado** |
19	| GMT-18 | Integrar el AWS SDK en el proyecto ASP.NET MVC para gestionar la subida y lectura de archivos | **Completado** |
20	| GMT-16 | Como usuario de GMT, quiero subir una foto de perfil para que mi identidad sea reconocible ante empresas o alumnos en la plataforma | **En progreso** |
21	| GMT-15 | Actualizar la tabla de Usuarios en PostgreSQL para incluir el estado de verificación y el token de seguridad | En curso |
21	| GMT-14 | Configurar el servicio SMTP (vía Amazon SES o Gmail) para el envío de correos de verificación de cuenta | **Completado** |
22	| GMT-13 | Implementar validación por expresión regular (Regex) en el formulario de registro para restringir el dominio de correo | En curso |
22	| GMT-12 | Como estudiante del Tecnológico de Acapulco, quiero registrarme exclusivamente con mi correo institucional (@acapulco.tecnm.mx) para asegurar que solo alumnos legítimos accedan a las vacantes de residencia | En curso |

<system-reminder>
Próximos pasos inmediatos

- **Validación de RFC pendiente**: La tarea GMT-11 (validar RFC con longitud exacta de 12 caracteres) sigue sin iniciar. Esta tarea es crítica antes de abrir el registro de empresas.
- **Configuración de Dominio**: Asegurar que el dominio institucional en los correos (actualmente @acapulco.tecnm.mx) esté correctamente configurado en el EmailService y en cualquier comunicación a empresas.
- **Integración de Empresas**: Continuar con la migración de la tabla Empresas y la validación de la constancia de Situación Fiscal.
- **Frontend de registro**: Implementar el selector entre registro como estudiante o empresa en el formulario de registro.