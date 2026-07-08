export interface NavigationItem {
  label: string;
  icon: string;
  route?: string;
  children?: NavigationItem[];
  enabled: boolean;
  badge?: string;
}

export interface NavigationSection {
  sectionTitle?: string;
  items: NavigationItem[];
}
