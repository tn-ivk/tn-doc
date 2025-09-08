using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Moq;
using NUnit.Framework;
using TN_Doc.Controllers;
using TN_DocGeneral.Services;

namespace Tests.Controllers;

/// <summary>
/// Набор тестов для PdfController
/// </summary>
[TestFixture]
public class PdfControllerTests
{
    private Mock<IReportBuffer> _mockReportBuffer;
    private PdfController _controller;

    [SetUp]
    public void Setup()
    {
        _mockReportBuffer = new Mock<IReportBuffer>();
        _controller = new PdfController(_mockReportBuffer.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    /// <summary>
    /// Get: при буфере null - возвращает NotFound()
    /// </summary>
    [Test]
    public void Get_BufferReturnsNull_ReturnsNotFound()
    {
        // Arrange
        _mockReportBuffer.Setup(x => x.GetPdfBytes()).Returns((byte[])null);

        // Act
        var result = _controller.Get();

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    /// <summary>
    /// Get: при пустом буфере - возвращает NotFound()
    /// </summary>
    [Test]
    public void Get_BufferReturnsEmptyArray_ReturnsNotFound()
    {
        // Arrange
        _mockReportBuffer.Setup(x => x.GetPdfBytes()).Returns(new byte[0]);

        // Act
        var result = _controller.Get();

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    /// <summary>
    /// Get: при наличии байт - возвращает FileContentResult с ContentType = "application/pdf"
    /// </summary>
    [Test]
    public void Get_BufferHasBytes_ReturnsFileContentResult()
    {
        // Arrange
        var testPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF header
        _mockReportBuffer.Setup(x => x.GetPdfBytes()).Returns(testPdfBytes);

        // Act
        var result = _controller.Get();

        // Assert
        Assert.That(result, Is.InstanceOf<FileContentResult>());
        var fileResult = result as FileContentResult;
        Assert.That(fileResult.ContentType, Is.EqualTo("application/pdf"));
        Assert.That(fileResult.FileContents, Is.EqualTo(testPdfBytes));
    }

    /// <summary>
    /// Get: при наличии байт - устанавливает анти-кеш заголовки
    /// </summary>
    [Test]
    public void Get_BufferHasBytes_SetsAntiCacheHeaders()
    {
        // Arrange
        var testPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF header
        _mockReportBuffer.Setup(x => x.GetPdfBytes()).Returns(testPdfBytes);

        // Act
        var result = _controller.Get();

        // Assert
        Assert.That(result, Is.InstanceOf<FileContentResult>());
        
        // Проверяем, что заголовки установлены
        Assert.That(_controller.Response.Headers.ContainsKey(HeaderNames.CacheControl), Is.True);
        Assert.That(_controller.Response.Headers[HeaderNames.CacheControl].ToString(), 
            Is.EqualTo("no-store, no-cache, must-revalidate"));
        
        Assert.That(_controller.Response.Headers.ContainsKey(HeaderNames.Pragma), Is.True);
        Assert.That(_controller.Response.Headers[HeaderNames.Pragma].ToString(), Is.EqualTo("no-cache"));
        
        Assert.That(_controller.Response.Headers.ContainsKey(HeaderNames.Expires), Is.True);
        Assert.That(_controller.Response.Headers[HeaderNames.Expires].ToString(), Is.EqualTo("0"));
    }

    /// <summary>
    /// Get: проверяет корректное взаимодействие с IReportBuffer
    /// </summary>
    [Test]
    public void Get_CallsGetPdfBytesOnce()
    {
        // Arrange
        var testPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF header
        _mockReportBuffer.Setup(x => x.GetPdfBytes()).Returns(testPdfBytes);

        // Act
        _controller.Get();

        // Assert
        _mockReportBuffer.Verify(x => x.GetPdfBytes(), Times.Once);
    }

    /// <summary>
    /// Get: проверяет поведение с различными размерами массива байт
    /// </summary>
    [Test]
    [TestCase(1)]
    [TestCase(100)]
    [TestCase(1024)]
    [TestCase(1048576)] // 1MB
    public void Get_VariousBufferSizes_ReturnsFileContentResult(int size)
    {
        // Arrange
        var testPdfBytes = new byte[size];
        for (int i = 0; i < size; i++)
        {
            testPdfBytes[i] = (byte)(i % 256);
        }
        _mockReportBuffer.Setup(x => x.GetPdfBytes()).Returns(testPdfBytes);

        // Act
        var result = _controller.Get();

        // Assert
        Assert.That(result, Is.InstanceOf<FileContentResult>());
        var fileResult = result as FileContentResult;
        Assert.That(fileResult.ContentType, Is.EqualTo("application/pdf"));
        Assert.That(fileResult.FileContents, Is.EqualTo(testPdfBytes));
        Assert.That(fileResult.FileContents.Length, Is.EqualTo(size));
    }

    /// <summary>
    /// Get: проверяет маршрутизацию - метод должен отвечать на GET /PDF/PDF.pdf
    /// </summary>
    [Test]
    public void Get_HasCorrectHttpGetAttribute()
    {
        // Arrange & Act
        var methodInfo = typeof(PdfController).GetMethod("Get");
        var attributes = methodInfo.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute), false);

        // Assert
        Assert.That(attributes.Length, Is.EqualTo(1));
        var httpGetAttribute = attributes[0] as Microsoft.AspNetCore.Mvc.HttpGetAttribute;
        Assert.That(httpGetAttribute.Template, Is.EqualTo("/PDF/PDF.pdf"));
    }
}