extern crate rust_library;

use remover::lib::{
    get_third_level_folder, git_commit, git_push, git_rm, has_readonly_permissions_recursive,
};
use rust_library::{is_valid_guid, is_valid_path};
use std::path::Path;
use structopt::StructOpt;

#[derive(StructOpt)]
struct CliApp {
    /// Violets guids
    #[structopt(long = "guids", short = "g", required = true, validator(is_valid_guid))]
    guids: Vec<String>,

    /// Path to folder with violets
    #[structopt(
        long = "path",
        short = "p",
        env = "PATH_TO_VIOLETS_DIR",
        validator(is_valid_path)
    )]
    path: String,

    /// Enable debug mode
    #[structopt(long = "debug")]
    debug: bool,
}

fn main() {
    let args = CliApp::from_args();

    let solution_dir = get_third_level_folder(Path::new(&args.path));
    if solution_dir.is_none() {
        eprintln!("Solution folder not found");
        std::process::exit(1)
    }

    for guid in &args.guids {
        let violet_dir = Path::new(&args.path).join(guid);

        if violet_dir.exists() == false {
            eprintln!("Folder '{}' doesn't exists", violet_dir.display());
            continue;
        }

        if has_readonly_permissions_recursive(&violet_dir) {
            eprintln!(
                "Folder '{}' has readonly files or folder in it",
                violet_dir.display()
            );
            continue;
        }

        match git_rm(solution_dir.unwrap(), violet_dir) {
            Ok(_) => {}
            Err(e) => eprintln!("{}", e),
        }
    }

    match git_commit(solution_dir.unwrap(), args.guids) {
        Ok(_) => {}
        Err(error) => {
            eprintln!("{}", error);
            std::process::exit(1);
        }
    }

    if let false = args.debug {
        match git_push(solution_dir.unwrap()) {
            Ok(_) => println!("Success"),
            Err(error) => {
                eprintln!("{}", error);
                std::process::exit(1);
            }
        }
    }

    std::process::exit(0)
}
