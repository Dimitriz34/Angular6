import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-no-access',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './no-access.html',
  styleUrls: ['./no-access.scss']
})
export class NoAccessComponent {}
