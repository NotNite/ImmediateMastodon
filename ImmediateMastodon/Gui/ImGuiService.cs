using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace ImmediateMastodon.Gui;

public class ImGuiService(ILogger<ImGuiService> logger, IHost host) : BackgroundService {
    private Sdl2Window window = null!;
    private GraphicsDevice graphicsDevice = null!;
    private ImGuiRenderer imguiRenderer = null!;
    private CommandList commandList = null!;

    public event Action<float>? OnDraw;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(() => {
        var width = 1280;
        var height = 720;

        VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo(
                40, 40, width, height,
                WindowState.Normal, "ImmediateMastodon"),
            out this.window,
            out this.graphicsDevice
        );

        this.window.Resized += this.HandleResize;
        this.graphicsDevice.SyncToVerticalBlank = true;

        this.imguiRenderer = new ImGuiRenderer(
            this.graphicsDevice,
            this.graphicsDevice.SwapchainFramebuffer.OutputDescription,
            width,
            height
        );
        this.commandList = this.graphicsDevice.ResourceFactory.CreateCommandList();

        var stopwatch = Stopwatch.StartNew();
        while (this.window.Exists && !stoppingToken.IsCancellationRequested) {
            var deltaTime = stopwatch.ElapsedTicks / (float) Stopwatch.Frequency;
            stopwatch.Restart();

            var snapshot = this.window.PumpEvents();
            if (!this.window.Exists) break;

            this.imguiRenderer.Update(deltaTime, snapshot);
            this.Draw(deltaTime);

            this.commandList.Begin();
            this.commandList.SetFramebuffer(this.graphicsDevice.MainSwapchain.Framebuffer);
            this.commandList.ClearColorTarget(0, RgbaFloat.Grey);

            this.imguiRenderer.Render(this.graphicsDevice, this.commandList);
            this.commandList.End();
            this.graphicsDevice.SubmitCommands(this.commandList);
            this.graphicsDevice.SwapBuffers(this.graphicsDevice.MainSwapchain);
        }

        this.graphicsDevice.WaitForIdle();
        this.window.Resized -= this.HandleResize;

        this.commandList.Dispose();
        this.imguiRenderer.Dispose();
        this.graphicsDevice.Dispose();
        this.window.Close();

        host.StopAsync(stoppingToken);
    }, stoppingToken);

    private void Draw(float deltaTime) {
        try {
            this.OnDraw?.Invoke(deltaTime);
        } catch (Exception e) {
            logger.LogError(e, "Error in OnDraw");
        }
    }

    public unsafe Texture LoadImage(byte[] data) {
        var img = SixLabors.ImageSharp.Image.Load<Rgba32>(data);
        var texture = this.graphicsDevice.ResourceFactory.CreateTexture(
            TextureDescription.Texture2D(
                (uint) img.Width,
                (uint) img.Height,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Sampled
            )
        );

        var size = texture.Width * texture.Height * 4;
        var span = new Span<byte>(new byte[size]);
        img.CopyPixelDataTo(span);
        var arr = span.ToArray();

        fixed (byte* bytes = arr) {
            this.graphicsDevice.UpdateTexture(
                texture,
                (nint) bytes,
                size,
                0,
                0,
                0,
                (uint) img.Width,
                (uint) img.Height,
                1,
                0,
                0
            );
        }

        return texture;
    }

    public nint GetTextureHandle(Texture texture) => this.imguiRenderer.GetOrCreateImGuiBinding(
        this.graphicsDevice.ResourceFactory,
        texture
    );

    public void RemoveTextureHandle(Texture texture) => this.imguiRenderer.RemoveImGuiBinding(texture);

    private void HandleResize() {
        this.graphicsDevice.MainSwapchain.Resize((uint) this.window.Width, (uint) this.window.Height);
        this.imguiRenderer.WindowResized(this.window.Width, this.window.Height);
    }
}
