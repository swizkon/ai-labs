using OpenAI.Files;
using OpenAI.Images;

namespace julkort2025;

public class AIImageEditor
{
    private readonly AppConfiguration _configuration;

    public AIImageEditor(AppConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task CreateXmasCard()
    {

        //OpenAIFileClient openAIFileClient = new(_configuration.OpenAIApiKey);

        //openAIFileClient.UploadFileAsync()

        // Generate AI version...
        ImageClient client = new("gpt-image-1-mini", _configuration.OpenAIApiKey);
        // ImageClient client = new("gpt-image-1", _configuration.OpenAIApiKey);
        string prompt =
            // "Task: Turn the persons and cats in the photo into a christmas card representing jesus in the manger using a glamorous style photo. "
            "Task: Create a christmas card representing jesus in the manger using a studio style photo. "
            
            + "The youngest girl should be lying in a manger and look very similiar to the photo. "
            
            + "The bold man should represent Jose, resemble the photo and look happy, wear hebrew clothes and be placed next to the manger. "
            
            + "The woman should represent Mary, look very similiar to the photo and look peaceful, wear christian clothes and be placed next to the manger. "
            //+ "The woman should represent Mary and be placed next to the manger. "

            + "The three boys should represent the three wise men, resemble the photo and look happy. Important that all of the triples boys are present in the final picture and resemble the boys from the input photo. "

            //+ "The cats should be added as having angel wings and hover above the stable."
            + "The cats should be looking like angels with wings and hover above the stable."
            
            + "Style: Realistic air brush. " // oil painting. " // Air brush
                                             // + "Style: Air brush. " // oil painting. " // Air brush

            //+ "Exaggeration: Emphasize the person's most distinctive features (eyes, nose, hair) while keeping recognizability. "

            // + "Body: Small, detailed body; casual stance with a christian hand gesture; Biblical era clothing. "
            + "Body: Formal stance with a christian hand gesture; Biblical era clothing. "

            + "Background: A night sky with a bright start and a stable with a banner with the text 'God Jul önskar fam Jerndin' in a handwriting-like font."
            + "Color: Vibrant yet natural, slightly amped skin tones. Lighting: Even, studio-like. Output: Full-body, landscape orientation. "
            + "Avoid: Photo background, watermarks, logos, straps, cropping. "
            + "Focus: Keep likeness and realism of the people from the input photo; Full body picture.";
        
        /*

        string prompt =
            // "Task: Turn the persons and cats in the photo into a christmas card representing jesus in the manger using a glamorous style photo. "
            "Task: Create a portrait representing jesus in the manger using a glamorous style photo. "
            // + "Must include three wise men, Mary, Joseph, baby Jesus and two cats."
            + "Use the faces of the people in the input. "
            + "The cats should be added as having angel wings and hover above the stable."

            + "Style: Futuristic air brush. " // oil painting. " // Air brush
                                              // + "Style: Air brush. " // oil painting. " // Air brush

            + "Exaggeration: Emphasize the person's most distinctive features (eyes, nose, hair) while keeping recognizability. "
            + "Body: Small, detailed body; casual stance with a christian hand gesture; Biblical era clothing. "
            + "Background: A night sky with a bright start and a stable with a banner with the text 'God Jul önskar fam Jerndin'."
            + "Color: Vibrant yet natural, slightly amped skin tones. Lighting: Even, studio-like. Output: Full-body, landscape orientation. "
            + "Avoid: Photo background, watermarks, logos, straps, cropping. "
            + "Focus: Keep likeness and realism of the people from the input photo; Full body picture.";
        */
        ImageEditOptions options = new()
        {
            // Quality = GeneratedImageQuality.Standard,
            Size = GeneratedImageSize.W1024xH1024,
            //Style = GeneratedImageStyle.Vivid,
            // ResponseFormat = GeneratedImageFormat.Bytes
        };

        Console.WriteLine("BEgin generate AI image");
        GeneratedImage image = await client.GenerateImageEditAsync(
            image: File.OpenRead(Path.Combine(_configuration.InputFolder, "IMG_hahaha2.png")),
            imageFilename: "IMG_hahaha2.png",
            prompt: prompt,
            options: options);

        BinaryData bytes = image.ImageBytes;

        // Store the AI image
        using (var s = bytes.ToStream())
        {
            var path = Path.Combine(_configuration.OutputFolder, $"ai_generated_{DateTime.Now:yyyyMMddHHmmss}.png");
            File.WriteAllBytes(path, bytes.ToArray());
            // url = await imageStorageService.UploadImageAsync(s, "ai/ai_" + imageId + ".png", "image/png");
        }
        Console.WriteLine("Done generate AI image");
    }
}

