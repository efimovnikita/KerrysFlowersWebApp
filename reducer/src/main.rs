extern crate exitcode;
use image::imageops::FilterType;
use image::DynamicImage;
use image::GenericImageView;
use std::path::PathBuf;
use std::process;
use std::thread;
use structopt::StructOpt;
use webp::*;

/// Tool for reduce image size
#[derive(StructOpt, Debug)]
#[structopt(name = "reducer")]
pub struct Args {
    /// Paths to images
    #[structopt(parse(from_os_str))]
    pub images: Vec<PathBuf>,
    /// Path to save folder
    #[structopt(short, long)]
    pub folder: PathBuf,
    /// Images quality
    #[structopt(short, long)]
    pub quality: Option<f32>,
}

struct Resolution {
    max_width: u32,
}

impl Resolution {
    fn save_to_webp(
        &self,
        img: &DynamicImage,
        image_path: &PathBuf,
        save_folder: &PathBuf,
        quality: f32,
    ) -> Result<PathBuf, std::io::Error> {
        let thumbnail = img.resize(self.max_width, 3000, FilterType::Lanczos3);

        let encoder: Encoder = Encoder::from_image(&thumbnail).unwrap();
        let webp: WebPMemory = encoder.encode(quality);

        let image_name = format!(
            "{}_{}.webp",
            image_path.file_stem().unwrap().to_str().unwrap(),
            self.max_width.to_string()
        );
        let img_path = save_folder.join(&image_name);
        let save_result = std::fs::write(&img_path, &*webp);
        match save_result {
            Ok(_) => Ok(img_path),
            Err(e) => Err(e),
        }
    }
}

fn main() {
    let args: Args = Args::from_args();

    if args.images.len() == 0 {
        eprintln!("Please provide some images");
        process::exit(exitcode::DATAERR)
    }

    for image in args.images.clone() {
        if image.exists() == false {
            eprintln!("Image file {} don't exist", image.display());
            process::exit(exitcode::IOERR)
        }
    }

    if args.folder.exists() == false {
        eprintln!("Folder for save images don't exist");
        process::exit(exitcode::IOERR)
    }

    let quality = match args.quality {
        Some(q) => q,
        None => 85.0,
    };

    if quality <= 0.0 {
        eprintln!("Quality less or equal to zero!");
        process::exit(exitcode::DATAERR)
    }

    if quality > 100.0 {
        eprintln!("Quality more than 100%!");
        process::exit(exitcode::DATAERR)
    }

    let mut threads = Vec::with_capacity(args.images.len());

    for image_path in args.images {
        // clone folder arg for threads
        let save_folder = args.folder.clone();

        // spawn new threads
        threads.push(thread::spawn(move || {

        let img_result = image::open(&image_path);
        if img_result.is_err() {
            eprintln!("Error while open image");
            process::exit(exitcode::IOERR)
        }

        let img = img_result.unwrap();

        let (w, h) = img.dimensions();
        if (w > 700 && h > 700) == false {
            eprintln!("Image too small: width - {}px, height - {}px. Width and height should be more than 700px", w, h);
            process::exit(exitcode::DATAERR)
        }

        let resolutions = [
            Resolution { max_width: 300 },
            Resolution { max_width: 330 },
            Resolution { max_width: 500 },
            Resolution { max_width: 700 },
        ];

        for resolution in resolutions {
            let result = resolution
            .save_to_webp(&img, &image_path, &save_folder, quality);

            match result {
                Ok(img_path) => {
                    if img_path.exists() == false {
                        eprintln!("Something went wrong when save file {}", img_path.display());
                        process::exit(exitcode::DATAERR)
                    }

                    println!("{}", img_path.display());
                }
                Err(e) => {
                    eprintln!("Error while save image: {}", e);
                    process::exit(exitcode::DATAERR)
                }
            }
        }
        }))
    }

    for handle in threads {
        handle.join().unwrap();
    }

    process::exit(exitcode::OK)
}
