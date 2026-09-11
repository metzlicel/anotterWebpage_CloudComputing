using anotterwebpage_WebApi.Domain;

namespace anotterwebpage.Domain.Tests;

public class CollabTests
{
    [Fact]
    public void Collab_CanBeCreatedWithExpectedValues()
    {
        // Act
        var collab = new Collab
        {
            Id = 1,
            Nombre = "Metzli",
            Apellido = "Lopez",
            NombreOrg = "CETYS",
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Propuesta de colaboración",
            CreatedAt = new DateTime(
                2026, 10, 1, 12, 0, 0,
                DateTimeKind.Utc)
        };

        // Assert
        Assert.Equal(1, collab.Id);
        Assert.Equal("Metzli", collab.Nombre);
        Assert.Equal("Lopez", collab.Apellido);
        Assert.Equal("CETYS", collab.NombreOrg);
        Assert.Equal("metzli@example.com", collab.Email);
        Assert.Equal("6641234567", collab.Numero);
        Assert.Equal(
            "Propuesta de colaboración",
            collab.Motivo);
    }
    
    // Optional fields
    [Fact]
    public void Collab_OptionalOrganization_CanBeNull()
    {
        // Act
        var collab = new Collab
        {
            Id = 1,
            Nombre = "Metzli",
            Apellido = "Lopez",
            NombreOrg = null,
            Email = "metzli@example.com",
            Numero = "6641234567",
            Motivo = "Colaboración"
        };

        // Assert
        Assert.Null(collab.NombreOrg);
    }
}