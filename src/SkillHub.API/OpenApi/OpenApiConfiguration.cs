using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using SkillHub.Application.DTOs;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SkillHub.API.OpenApi;

public sealed class OpenApiDocumentOrderFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new HashSet<OpenApiTag>
        {
            CreateOrderedTag(
                "Auth",
                "1. Public registration and JWT authentication",
                ["01_RegisterParticipant", "02_Login"]),
            CreateOrderedTag(
                "Workshops",
                "2. Public workshop catalog and Employee workshop management",
                ["03_GetUpcomingWorkshops", "04_GetWorkshopById", "05_CreateWorkshop", "06_GetWorkshopEnrollments"]),
            CreateOrderedTag(
                "Enrollments",
                "3. Participant enrollments, cancellations, Excel bulk import, and PDF certificates",
                ["07_CreateEnrollment", "08_GetMyEnrollments", "09_CancelEnrollment", "10_ImportEnrollments", "10b_DownloadEnrollmentTemplate", "11_DownloadCertificate"]),
            CreateOrderedTag(
                "Reports",
                "4. Employee reports ordered by active enrollments",
                ["12_GetTopWorkshops", "12b_ExportTopWorkshopsExcel"])
        };
    }

    private static OpenApiTag CreateOrderedTag(string name, string description, string[] operationIds)
    {
        var orderArray = new JsonArray();
        foreach (var id in operationIds)
        {
            orderArray.Add(id);
        }

        return new OpenApiTag
        {
            Name = name,
            Description = description,
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                ["x-scalar-order"] = new JsonNodeExtension(orderArray)
            }
        };
    }
}

public sealed class OpenApiRequestExamplesFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema)
            return;

        if (context.Type == typeof(RegisterRequestDto))
        {
            concreteSchema.Examples =
            [
                new JsonObject
                {
                    ["fullName"] = "Camila Andrea Gómez",
                    ["email"] = "camila.gomez@correo.com",
                    ["password"] = "Participant2026*"
                }
            ];
        }
        else if (context.Type == typeof(LoginRequestDto))
        {
            concreteSchema.Examples =
            [
                new JsonObject
                {
                    ["email"] = "andres.mejia@correo.com",
                    ["password"] = "Instructor2026*"
                }
            ];
        }
        else if (context.Type == typeof(CreateWorkshopRequestDto))
        {
            concreteSchema.Examples =
            [
                new JsonObject
                {
                    ["code"] = "FOT-010",
                    ["title"] = "Night Street Photography",
                    ["description"] = "Urban lighting, composition, and long exposure techniques",
                    ["instructorEmail"] = "andres.mejia@correo.com",
                    ["startDateTime"] = "2027-03-15T14:00:00Z",
                    ["endDateTime"] = "2027-03-15T18:00:00Z",
                    ["capacity"] = 15
                }
            ];
        }
        else if (context.Type == typeof(CreateEnrollmentRequestDto))
        {
            concreteSchema.Examples =
            [
                new JsonObject
                {
                    ["workshopId"] = "00000000-0000-0000-0000-000000000000"
                }
            ];
        }
    }
}
