export interface Slide {
  title: string;
  description?: string;
  code?: string;
  gifPath?: string;
  durationInSeconds: number;
  outro?: boolean;
  outroLines?: string[];
  collage?: boolean;
  skipEntrance?: boolean;
}

export interface ReelConfig {
  fps: number;
  slides: Slide[];
  background?: string;
  logoPath?: string;
  logoPathDark?: string;
  brandTagline?: string;
  clockfaceName?: string;
  footerLeft?: string;
  footerRight?: string;
  outroPhotos?: string[];
}
