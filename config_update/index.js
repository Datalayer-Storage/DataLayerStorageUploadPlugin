const yaml = require("js-yaml");
const fs = require("fs");
const os = require("os");
const path = require("path");

// Get the home directory
const user_home_dir = os.homedir();

const chia_config_path = path.join(
  user_home_dir,
  ".chia/mainnet/config/config.yaml"
);
const chia_config_backup_path = path.join(
  user_home_dir,
  ".chia/mainnet/config/config.yaml.bak"
);

// Copy the original config.yaml to config.yaml.bak
fs.copyFileSync(chia_config_path, chia_config_backup_path);

const data = yaml.load(fs.readFileSync(chia_config_path, "utf8"));

if (!data.hasOwnProperty("data_layer")) {
  data["data_layer"] = { uploaders: [] };
}

if (!data.data_layer.hasOwnProperty("uploaders")) {
  data.data_layer.uploaders = [];
}

if (!data.data_layer.uploaders.includes("http://localhost:41410")) {
  data.data_layer.uploaders.push("http://localhost:41410");
}

fs.writeFileSync(chia_config_path, yaml.dump(data));
