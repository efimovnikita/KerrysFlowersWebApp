extern crate exitcode;
use clap::Parser;
use image::imageops::FilterType;
use image::GenericImageView;
use std::path::PathBuf;
use std::process;
use uuid::Uuid;
use webp::*;

/// Tool for reduce image size
#[derive(Parser, Debug)]
#[command(author, version, about, long_about = None)]
pub struct Args {
    /// Paths to images
    #[clap(short, long)]
    pub images: Vec<PathBuf>,
    /// Path to save folder
    #[clap(short, long)]
    pub folder: PathBuf,
    /// Images quality
    #[clap(long)]
    pub quality: Option<f32>,
}

struct Resolution {
    width: u32,
}

fn main() {
    let args: Args = Args::parse();

    for image in args.images.clone() {
        if image.exists() == false {
            println!("Image file {} don't exist", image.display());
            process::exit(exitcode::IOERR)
        }
    }

    if args.folder.exists() == false {
        println!("Folder for save images don't exist");
        process::exit(exitcode::IOERR)
    }

    let quality = match args.quality {
        Some(q) => q,
        None => 85.0,
    };

    if quality <= 0.0 {
        println!("Quality less or equal to zero!");
        process::exit(exitcode::DATAERR)
    }

    if quality > 100.0 {
        println!("Quality more than 100%!");
        process::exit(exitcode::DATAERR)
    }

    for image in args.images {
        let id = Uuid::new_v4().to_string();

        let img_result = image::open(&image);
        if img_result.is_err() {
            println!("Error while open image");
            process::exit(exitcode::IOERR)
        }

        let img = img_result.unwrap();

        let (w, h) = img.dimensions();
        if (w > 700 && h > 700) == false {
            println!("Image too small: width - {}px, height - {}px. Width and height should be more than 700px", w, h);
            process::exit(exitcode::DATAERR)
        }

        let resolutions = [
            Resolution { width: 300 },
            Resolution { width: 330 },
            Resolution { width: 500 },
            Resolution { width: 700 },
        ];

        for resolution in resolutions {
            let thumbnail = img.resize(resolution.width, 3000, FilterType::Lanczos3);

            let encoder: Encoder = Encoder::from_image(&thumbnail).unwrap();
            let webp: WebPMemory = encoder.encode(quality);

            let image_name = format!("{}_{}.webp", id, resolution.width.to_string());
            let img_path = args.folder.join(&image_name);
            let save_result = std::fs::write(&img_path, &*webp);
            match save_result {
                Ok(_) => {
                    if img_path.exists() == false {
                        println!("Something went wrong when save file {}", img_path.display());
                        process::exit(exitcode::DATAERR)
                    }

                    let parent_dir = img_path.parent().unwrap();
                    let sub_parent_dir = parent_dir.parent().unwrap();

                    println!(
                        "{}/{}/{}",
                        sub_parent_dir.file_name().unwrap().to_str().unwrap(),
                        parent_dir.file_name().unwrap().to_str().unwrap(),
                        image_name
                    );
                }
                Err(e) => {
                    println!("Error while save image: {}", e);
                    process::exit(exitcode::DATAERR)
                }
            }
        }
    }

    process::exit(exitcode::OK)
}
