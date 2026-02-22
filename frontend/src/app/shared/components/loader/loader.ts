import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoadingService } from '../../../services/loading';
import { IMAGE_PATHS } from '../../constants/image-paths';

@Component({
  selector: 'app-loader',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './loader.html',
  styleUrl: './loader.scss'
})
export class LoaderComponent {
  public loadingService = inject(LoadingService);
  public readonly IMAGE_PATHS = IMAGE_PATHS;
}
