import Phaser from "phaser";
import {
  AvailableActionKind,
  BuildingKind,
  BuildingView,
  TownProsperity,
  type TownLayoutDto,
} from "../src/api/types";
import { TownHubScene } from "../src/components/town-hub/TownHubScene";

const layout: TownLayoutDto = {
  buildings: [
    {
      kind: BuildingKind.Store,
      view: BuildingView.Profile,
      x: 50,
      y: 50,
      width: 24,
      height: 30,
    },
  ],
  playerSpawnX: 5,
  playerSpawnY: 5,
  prosperity: TownProsperity.Prosperous,
  paths: [],
  tileGrid: Array.from({ length: 10 }, () => Array<number>(10).fill(0)),
};

class BrowserTownHubScene extends TownHubScene {
  override create() {
    super.create();
    document.documentElement.dataset.storeSpriteLoaded = String(
      this.textures.exists(`building-${BuildingKind.Store}`),
    );
  }
}

const scene = new BrowserTownHubScene(layout, [AvailableActionKind.BuySupplies], () => undefined);

new Phaser.Game({
  parent: document.getElementById("game") ?? undefined,
  width: 800,
  height: 500,
  backgroundColor: "#c4a87a",
  scene,
  scale: {
    mode: Phaser.Scale.FIT,
    autoCenter: Phaser.Scale.CENTER_BOTH,
  },
});
