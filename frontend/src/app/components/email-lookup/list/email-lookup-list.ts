import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { EmailLookupService } from '../../../services/email-lookup';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { EmailServiceLookup } from '../../../models/email-lookup.model';

@Component({
  selector: 'app-email-lookup-list',
  standalone: true,
  imports: [CommonModule, RouterModule, PaginationComponent],
  templateUrl: './email-lookup-list.html',
  styleUrl: './email-lookup-list.scss'
})
export class EmailLookupListComponent implements OnInit {
  private emailLookupService = inject(EmailLookupService);

  allEmailServices: EmailServiceLookup[] = [];
  emailServices: EmailServiceLookup[] = [];
  currentPage: number = 1;
  pageSize: number = 5;
  count: number = 0;

  async ngOnInit(): Promise<void> {
    await this.loadEmailServices();
  }

  async loadEmailServices(): Promise<void> {
    try {
      const response: any = await firstValueFrom(this.emailLookupService.getEmailServices(1, 1000));
      if (response && (response.data || response.resultData)) {
        // Handle both PaginatedResult (.data) and ApiResponse (.resultData)
        const services = response.data || (response.resultData && response.resultData.length > 0 ? response.resultData : []);
        // Filter out ExchangeServer, Test_ES, TP Internal, and Exchange Server
        this.allEmailServices = services
          .filter((s: any) => 
            s.serviceName !== 'ExchangeServer' && 
            s.serviceName !== 'Test_ES' &&
            s.serviceName !== 'TP Internal' &&
            s.serviceName !== 'Exchange Server'
          )
          .sort((a: any, b: any) => a.serviceName.localeCompare(b.serviceName));
        this.count = this.allEmailServices.length;
        this.applyPagination();
      }
    } catch (error) {
      console.error('Error loading email services:', error);
    }
  }

  applyPagination() {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    this.emailServices = this.allEmailServices.slice(startIndex, endIndex);
  }

  onPageChange(page: number) {
    this.currentPage = page;
    this.applyPagination();
  }
}
