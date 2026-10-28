# SkillHub — Guía Maestra de Sustentación Técnica (20/20 Puntos)

**Autor:** Jose Diaz  
**Email:** `josedelcarmen_diaz@outlook.com`  
**Clan:** Esthersita  
**Cédula:** `1045755521`  
**Criterio de Evaluación:** *Sustentación (20 Puntos: "Argumenta y defiende de manera clara y argumentada la lógica implementada, evidenciando dominio del tema y capacidad de toma de decisiones técnicas avanzadas.")*

---

## 1. Justificación Arquitectónica: Clean Architecture & DDD

### ¿Por qué Clean Architecture y no una arquitectura de 3 capas tradicional?
> En una arquitectura de 3 capas tradicional, la lógica de negocio suele depender de la base de datos (acoplamiento directo con Entity Framework o ADO.NET). En **Clean Architecture**, aplicamos el **Principio de Inversión de Dependencias (DIP)**:
- **`SkillHub.Domain` es el núcleo absoluto:** Contiene entidades (`Workshop`, `Enrollment`, `Instructor`, `Participant`), Value Objects y reglas de negocio puras. Posee **cero referencias externas** (`0 PackageReferences`). No conoce si los datos se guardan en PostgreSQL, MongoDB o memoria.
- **`SkillHub.Application` orquesta los casos de uso:** Define DTOs, validaciones con FluentValidation, interfaces de repositorios (`IWorkshopRepository`, `IEnrollmentRepository`, `IUnitOfWork`) y servicios de aplicación.
- **`SkillHub.Infrastructure` implementa los detalles tecnológicos:** Configura EF Core con Npgsql (PostgreSQL), ASP.NET Core Identity, generación de Excel con EPPlus 8, PDFs con iText 9 y envío de correos con MailKit.
- **`SkillHub.API` es un puerto de entrada:** Controladores delgados (Thin Controllers) que reciben peticiones HTTP, delegan a la capa de aplicación y retornan respuestas estandarizadas bajo RFC 7807.

### ¿Cómo se evidencia Domain-Driven Design (DDD)?
- **Modelos de Dominio Ricos (evitando el modelo anémico):** Las entidades controlan su propio estado. Por ejemplo, `Workshop.CreateNew(...)` valida la capacidad y fechas; `Enrollment.Cancel(...)` aplica la regla de negocio que prohíbe cancelar después de iniciado el taller.
- **Value Objects Inmutables:** `EmailAddress`, `WorkshopCode` y `TimeSlot` aseguran que ningún objeto del sistema pueda existir en un estado inválido.
- **Unit of Work & Repository Pattern:** Abstraen el motor de persistencia, facilitando pruebas unitarias con mocks (`Moq`) sin necesidad de tocar la base de datos real.

---

## 2. API REST, Seguridad & Correo SMTP

### ¿Cómo opera el flujo de Autoregistro, JWT y Correo SMTP?
1. **Petición del Empleado:** Llega al endpoint público `POST /api/v1/auth/register`.
2. **Validación:** FluentValidation valida formato de email, longitud de clave y campos obligatorios.
3. **Persistencia Transaccional:** Se crea el usuario en `Microsoft.AspNetCore.Identity` y la entidad `Participant` vinculada en PostgreSQL bajo una transacción atómica.
4. **Generación de JWT:** Se emite un token firmado con algoritmo HMAC-SHA256 (`JwtTokenService`) que incluye los claims: `NameIdentifier`, `Email`, `Role` (`Participant`), y `ParticipantId`.
5. **Notificación Asíncrona por Correo (MailKit):** Se invoca `_emailService.SendWelcomeEmailAsync(...)` generando una plantilla HTML institucional responsive.
6. **Resiliencia ante Fallos de Red:** Si el servidor SMTP se encuentra fuera de línea o sin conectividad externa, el servicio captura la excepción mediante un bloque resiliente, registra un log de advertencia estructurado (`_logger.LogWarning`) y **permite que el autoregistro culmine exitosamente sin arrojar un error 500 al usuario**.

---

## 3. Funcionalidades de Administración (RRHH)

### A. Importación Masiva desde Excel (EPPlus 8)
- **Tolerancia a Fallos Fila por Fila:** La importación no se aborta si una fila falla. Se procesan todas las filas del archivo Excel, discriminando entre:
  - Registros exitosos (`EnrolledCount`).
  - Registros rechazados (`RejectedRows`) con el número de fila exacto y el motivo técnico (taller no encontrado, horario conflictivo, capacidad agotada, email no registrado).
- **Consistencia en Memoria:** Si una fila falla, se ejecuta `_unitOfWork.ClearTrackedChanges()` para evitar que entidades corruptas se queden en el ChangeTracker de EF Core.

### B. Constancia Oficial de Inscripción en PDF (iText 9)
- **Diseño Membretado Institucional:** Alejado de facturas DIAN o diplomas genéricos; implementa un formato auténtico de constancia académica/laboral con número de radicado oficial (`SH-XXXXXXXX`).
- **Garantía Estricta de 1 Página:** Márgenes calculados milimétricamente (`24pt` superior, `20pt` inferior, `38pt` laterales) asegurando que el contenido nunca desborde a una segunda hoja.
- **Mecanismos de Validación:** Incorpora código de barras bidimensional QR escaneable con URL de verificación y un hash criptográfico SHA-256 para auditoría de integridad documental.

### C. Reporte y Dashboard en Excel
- Exporta tanto plantillas de carga con listas desplegables de talleres activos como reportes de ocupación con KPIs consolidados.

---

## 4. Dockerización & Despliegue

### ¿Por qué una compilación multi-etapa (Multi-Stage Build)?
- **`Stage 1 (build)` con `sdk:10.0`:** Restaura paquetes y compila la solución en modo Release.
- **`Stage 2 (runtime)` con `aspnet:10.0`:** Contiene exclusivamente el runtime liviano de producción, reduciendo el tamaño de la imagen final a una fracción y eliminando herramientas de compilación que podrían representar vectores de ataque.
- **Orquestación con Docker Compose:**
  - `db`: PostgreSQL 16 Alpine con volume persistente (`skillhub_pgdata`) y healthcheck activo (`pg_isready`).
  - `mailpit`: Servidor SMTP local en el puerto 1025 y bandeja web interactiva en `http://localhost:8025`.
  - `api`: Depende del estado saludable (`service_healthy`) de `db`, ejecuta las migraciones automáticas y el seeding de datos al iniciar.

---

## 5. Preguntas Frecuentes del Evaluador

| Pregunta Potencial | Respuesta Técnica Rápida |
| :--- | :--- |
| *¿Cómo controlaron que no haya talleres simultáneos para el mismo empleado?* | En `EnrollmentService`, se consultan las inscripciones activas del participante y se valida con `workshop.OverlapsWith(...)` que ningún intervalo de fechas (`TimeSlot`) colisione. |
| *¿Cómo previenen inyecciones SQL?* | Todo el acceso a datos se realiza mediante Entity Framework Core con consultas parametrizadas. Ninguna sentencia SQL es concatenada manualmente. |
| *¿Cómo aseguran la vigencia del token JWT?* | Se valida tiempo de expiración (`ValidateLifetime = true`), emisor (`ValidateIssuer = true`), audiencia (`ValidateAudience = true`) y clave criptográfica simétrica (`ValidateIssuerSigningKey = true`) con un ClockSkew mínimo de 1 minuto. |
| *¿Qué pasa si la base de datos se cae?* | La API implementa el middleware `GlobalExceptionHandler` que intercepta excepciones de red y conexión, retornando un `ProblemDetails` con código HTTP 500 y formato RFC 7807 sin filtrar cadenas de conexión ni contraseñas. |
